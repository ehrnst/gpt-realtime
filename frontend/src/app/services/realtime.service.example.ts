// REFERENCE IMPLEMENTATION - This file shows key changes needed in realtime.service.ts
// The main change is updating the connect() method signature and token fetching logic

import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class RealtimeService {
  private peerConnection: RTCPeerConnection | null = null;
  private dataChannel: RTCDataChannel | null = null;
  private localStream: MediaStream | null = null;
  
  public messages$ = new Subject<any>();
  public connectionState$ = new Subject<string>();

  constructor() {}

  // UPDATED: Method now accepts optional personaId parameter
  async connect(personaId?: string): Promise<void> {
    try {
      this.connectionState$.next('connecting');

      // UPDATED: Build token URL with personaId if provided
      const tokenUrl = personaId 
        ? `${environment.apiUrl}/api/token?personaId=${encodeURIComponent(personaId)}`
        : `${environment.apiUrl}/api/token`;

      // Fetch session token
      const tokenResponse = await fetch(tokenUrl);
      if (!tokenResponse.ok) {
        throw new Error(`Failed to get session token: ${tokenResponse.statusText}`);
      }

      const sessionToken = await tokenResponse.json();
      const { clientSecret, realtimeUrl } = sessionToken;

      // Create peer connection
      this.peerConnection = new RTCPeerConnection({
        iceServers: [{ urls: 'stun:stun.l.google.com:19302' }]
      });

      // Set up data channel for messages
      this.dataChannel = this.peerConnection.createDataChannel('oai-events', {
        ordered: true
      });

      this.setupDataChannel();

      // Get microphone access
      this.localStream = await navigator.mediaDevices.getUserMedia({ 
        audio: true 
      });

      // Add audio track (initially disabled)
      const audioTrack = this.localStream.getAudioTracks()[0];
      if (audioTrack) {
        audioTrack.enabled = false;
        this.peerConnection.addTrack(audioTrack, this.localStream);
      }

      // Create and set local description
      const offer = await this.peerConnection.createOffer();
      await this.peerConnection.setLocalDescription(offer);

      // Send offer to Azure OpenAI
      const model = 'gpt-4o-realtime-preview';
      const sdpResponse = await fetch(
        `${realtimeUrl}?model=${model}`,
        {
          method: 'POST',
          headers: {
            'Authorization': `Bearer ${clientSecret}`,
            'Content-Type': 'application/sdp'
          },
          body: offer.sdp
        }
      );

      if (!sdpResponse.ok) {
        throw new Error(`SDP exchange failed: ${sdpResponse.statusText}`);
      }

      const answerSdp = await sdpResponse.text();
      await this.peerConnection.setRemoteDescription({
        type: 'answer',
        sdp: answerSdp
      });

      this.connectionState$.next('connected');
    } catch (error) {
      this.connectionState$.next('failed');
      console.error('Connection error:', error);
      throw error;
    }
  }

  private setupDataChannel(): void {
    if (!this.dataChannel) return;

    this.dataChannel.onopen = () => {
      console.log('Data channel opened');
    };

    this.dataChannel.onmessage = (event) => {
      try {
        const message = JSON.parse(event.data);
        this.messages$.next(message);
        
        // Handle session created event
        if (message.type === 'session.created') {
          // Trigger initial greeting
          this.sendMessage({
            type: 'response.create'
          });
        }
      } catch (error) {
        console.error('Error parsing message:', error);
      }
    };

    this.dataChannel.onerror = (error) => {
      console.error('Data channel error:', error);
    };

    this.dataChannel.onclose = () => {
      console.log('Data channel closed');
      this.connectionState$.next('disconnected');
    };
  }

  startAudioCapture(): void {
    if (this.localStream) {
      const audioTrack = this.localStream.getAudioTracks()[0];
      if (audioTrack) {
        audioTrack.enabled = true;
      }
    }
  }

  stopAudioCapture(): void {
    if (this.localStream) {
      const audioTrack = this.localStream.getAudioTracks()[0];
      if (audioTrack) {
        audioTrack.enabled = false;
      }
    }
    
    // Commit audio buffer
    this.sendMessage({
      type: 'input_audio_buffer.commit'
    });
    
    // Request response
    this.sendMessage({
      type: 'response.create'
    });
  }

  sendMessage(message: any): void {
    if (this.dataChannel && this.dataChannel.readyState === 'open') {
      this.dataChannel.send(JSON.stringify(message));
    }
  }

  disconnect(): void {
    if (this.localStream) {
      this.localStream.getTracks().forEach(track => track.stop());
      this.localStream = null;
    }

    if (this.dataChannel) {
      this.dataChannel.close();
      this.dataChannel = null;
    }

    if (this.peerConnection) {
      this.peerConnection.close();
      this.peerConnection = null;
    }

    this.connectionState$.next('disconnected');
  }
}
