import { Injectable } from '@angular/core';
import { Observable, Subject } from 'rxjs';

export interface RealtimeMessage {
  type: string;
  [key: string]: any;
}

@Injectable({
  providedIn: 'root'
})
export class RealtimeService {
  private peerConnection: RTCPeerConnection | null = null;
  private eventsChannel: RTCDataChannel | null = null;
  private localStream: MediaStream | null = null;
  private remoteAudioElement: HTMLAudioElement | null = null;
  private messageSubject = new Subject<RealtimeMessage>();
  private sessionInstructions: string = '';
  private voiceSetting: string = 'alloy';

  async connect(
    clientSecret: string,
    realtimeUrl: string,
    audioElement: HTMLAudioElement,
    systemInstructions?: string,
    voice?: string
  ): Promise<Observable<RealtimeMessage>> {
    this.disconnect();

    this.remoteAudioElement = audioElement;
    this.messageSubject = new Subject<RealtimeMessage>();
    
    // Store the instructions and voice from backend
    this.sessionInstructions = systemInstructions || 'You are a helpful AI assistant.';
    this.voiceSetting = voice || 'alloy';

    this.peerConnection = new RTCPeerConnection({
      iceServers: [{ urls: 'stun:stun.l.google.com:19302' }]
    });

    this.peerConnection.oniceconnectionstatechange = () => {
      this.messageSubject.next({
        type: 'peer.ice_state',
        state: this.peerConnection?.iceConnectionState
      });
    };

    this.peerConnection.ontrack = (event) => {
      const [stream] = event.streams;
      if (stream && this.remoteAudioElement) {
        this.remoteAudioElement.srcObject = stream;
        this.remoteAudioElement.play().catch(() => {
          // Autoplay might fail without user interaction; ignore.
        });
      }
    };

    this.peerConnection.ondatachannel = (event) => {
      this.configureDataChannel(event.channel);
    };

    this.eventsChannel = this.peerConnection.createDataChannel('oai-events');
    this.configureDataChannel(this.eventsChannel);

    this.localStream = await navigator.mediaDevices.getUserMedia({ audio: true, video: false });
    this.localStream.getAudioTracks().forEach((track) => {
      track.enabled = false; // Start muted until recording begins.
      this.peerConnection?.addTrack(track, this.localStream as MediaStream);
    });

    const offer = await this.peerConnection.createOffer();
    await this.peerConnection.setLocalDescription(offer);

    // For Azure WebRTC, we need to add the deployment parameter to the URL
    const webrtcUrlWithDeployment = realtimeUrl.includes('?') 
      ? `${realtimeUrl}&model=gpt-4o-realtime-preview` 
      : `${realtimeUrl}?model=gpt-4o-realtime-preview`;

    const response = await fetch(webrtcUrlWithDeployment, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${clientSecret}`,
        'Content-Type': 'application/sdp'
      },
      body: offer.sdp ?? ''
    });

    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`Realtime offer failed: ${response.status} ${errorText}`);
    }

    const answer = await response.text();
    await this.peerConnection.setRemoteDescription({ type: 'answer', sdp: answer });

    return this.messageSubject.asObservable();
  }

  disconnect(): void {
    if (this.eventsChannel) {
      this.eventsChannel.close();
      this.eventsChannel = null;
    }

    if (this.peerConnection) {
      this.peerConnection.getSenders().forEach((sender) => sender.track?.stop());
      this.peerConnection.close();
      this.peerConnection = null;
    }

    if (this.localStream) {
      this.localStream.getTracks().forEach((track) => track.stop());
      this.localStream = null;
    }

    if (this.remoteAudioElement) {
      this.remoteAudioElement.srcObject = null;
      this.remoteAudioElement = null;
    }
    this.messageSubject.complete();
    this.messageSubject = new Subject<RealtimeMessage>();
  }

  async startAudioCapture(): Promise<void> {
    if (!this.localStream) {
      throw new Error('No local media stream available. Connect first.');
    }

    if (!this.eventsChannel || this.eventsChannel.readyState !== 'open') {
      throw new Error('Realtime channel is not ready yet.');
    }

    this.localStream.getAudioTracks().forEach((track) => {
      track.enabled = true;
    });

    this.sendEvent({ type: 'input_audio_buffer.start' });
  }

  stopAudioCapture(): void {
    if (!this.localStream) {
      return;
    }

    if (!this.eventsChannel || this.eventsChannel.readyState !== 'open') {
      return;
    }

    this.localStream.getAudioTracks().forEach((track) => {
      track.enabled = false;
    });

    this.sendEvent({ type: 'input_audio_buffer.commit' });
    this.sendEvent({ type: 'response.create' });
  }

  sendEvent(event: RealtimeMessage): void {
    if (this.eventsChannel?.readyState === 'open') {
      this.eventsChannel.send(JSON.stringify(event));
    }
  }

  private triggerGreeting(): void {
    // Request an immediate response from the assistant without custom instructions
    // This lets the assistant use the session instructions for greeting
    console.log('Triggering greeting response');
    this.sendEvent({
      type: 'response.create'
    });
  }

  private sendSessionUpdate(): void {
    // Send session update to initialize the conversation
    console.log('Sending session update with instructions:', this.sessionInstructions);
    this.sendEvent({
      type: 'session.update',
      session: {
        modalities: ['text', 'audio'],
        instructions: this.sessionInstructions,
        voice: this.voiceSetting,
        input_audio_format: 'pcm16',
        output_audio_format: 'pcm16',
        input_audio_transcription: {
          model: 'whisper-1'
        },
        turn_detection: {
          type: 'server_vad',
          threshold: 0.5,
          prefix_padding_ms: 300,
          silence_duration_ms: 500
        }
      }
    });
  }

  private configureDataChannel(channel: RTCDataChannel): void {
    this.eventsChannel = channel;

    channel.onmessage = (event) => {
      try {
        const payload = JSON.parse(event.data);
        this.messageSubject.next(payload);
        
        // When session is updated (after our session.update), trigger greeting
        if (payload.type === 'session.updated') {
          console.log('Session updated, triggering greeting');
          setTimeout(() => {
            this.triggerGreeting();
          }, 100); // Small delay to ensure everything is ready
        }
      } catch (error) {
        console.error('Failed to parse realtime message', error);
      }
    };

    channel.onopen = () => {
      this.messageSubject.next({ type: 'peer.data_channel', state: 'open' });
      
      // Send session update after data channel opens
      this.sendSessionUpdate();
    };

    channel.onclose = () => {
      this.messageSubject.next({ type: 'peer.data_channel', state: 'closed' });
    };

    channel.onerror = (error) => {
      console.error('Realtime data channel error', error);
      this.messageSubject.next({ type: 'peer.data_channel', state: 'error', error });
    };
  }
}
