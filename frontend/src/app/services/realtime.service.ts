import { Injectable } from '@angular/core';
import { Subject, Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface RealtimeMessage {
  type: string;
  [key: string]: any;
}

@Injectable({
  providedIn: 'root'
})
export class RealtimeService {
  private webSocket: WebSocket | null = null;
  private messageSubject = new Subject<RealtimeMessage>();
  private audioContext: AudioContext | null = null;
  private mediaStream: MediaStream | null = null;
  private audioWorkletNode: AudioWorkletNode | null = null;

  constructor() { }

  connect(token: string): Observable<RealtimeMessage> {
    const wsUrl = environment.apiUrl.replace('http://', 'ws://').replace('https://', 'wss://');
    this.webSocket = new WebSocket(`${wsUrl}/api/realtime/ws?token=${token}`);

    this.webSocket.onopen = () => {
      console.log('WebSocket connected');
    };

    this.webSocket.onmessage = (event) => {
      try {
        const message = JSON.parse(event.data);
        this.messageSubject.next(message);
        this.handleMessage(message);
      } catch (error) {
        console.error('Error parsing WebSocket message:', error);
      }
    };

    this.webSocket.onerror = (error) => {
      console.error('WebSocket error:', error);
    };

    this.webSocket.onclose = () => {
      console.log('WebSocket closed');
      this.cleanup();
    };

    return this.messageSubject.asObservable();
  }

  disconnect(): void {
    if (this.webSocket) {
      this.webSocket.close();
      this.webSocket = null;
    }
    this.cleanup();
  }

  sendMessage(message: RealtimeMessage): void {
    if (this.webSocket && this.webSocket.readyState === WebSocket.OPEN) {
      this.webSocket.send(JSON.stringify(message));
    }
  }

  async startAudioCapture(): Promise<void> {
    try {
      this.mediaStream = await navigator.mediaDevices.getUserMedia({ audio: true });
      this.audioContext = new AudioContext({ sampleRate: 24000 });
      
      const source = this.audioContext.createMediaStreamSource(this.mediaStream);
      
      // Create a script processor for now (AudioWorklet would be better for production)
      const processor = this.audioContext.createScriptProcessor(4096, 1, 1);
      
      processor.onaudioprocess = (e) => {
        const inputData = e.inputBuffer.getChannelData(0);
        const pcm16 = this.floatTo16BitPCM(inputData);
        const base64Audio = this.arrayBufferToBase64(pcm16.buffer);
        
        this.sendMessage({
          type: 'input_audio_buffer.append',
          audio: base64Audio
        });
      };

      source.connect(processor);
      processor.connect(this.audioContext.destination);
    } catch (error) {
      console.error('Error starting audio capture:', error);
      throw error;
    }
  }

  stopAudioCapture(): void {
    if (this.mediaStream) {
      this.mediaStream.getTracks().forEach(track => track.stop());
      this.mediaStream = null;
    }
    if (this.audioContext) {
      this.audioContext.close();
      this.audioContext = null;
    }
  }

  private handleMessage(message: RealtimeMessage): void {
    switch (message.type) {
      case 'response.audio.delta':
        if (message['delta']) {
          this.playAudioDelta(message['delta']);
        }
        break;
      case 'error':
        console.error('Realtime API error:', message);
        break;
    }
  }

  private playAudioDelta(base64Audio: string): void {
    try {
      const audioData = this.base64ToArrayBuffer(base64Audio);
      if (!this.audioContext) {
        this.audioContext = new AudioContext({ sampleRate: 24000 });
      }
      
      const int16Array = new Int16Array(audioData);
      const float32Array = new Float32Array(int16Array.length);
      
      for (let i = 0; i < int16Array.length; i++) {
        float32Array[i] = int16Array[i] / 32768.0;
      }
      
      const audioBuffer = this.audioContext.createBuffer(1, float32Array.length, 24000);
      audioBuffer.getChannelData(0).set(float32Array);
      
      const source = this.audioContext.createBufferSource();
      source.buffer = audioBuffer;
      source.connect(this.audioContext.destination);
      source.start();
    } catch (error) {
      console.error('Error playing audio:', error);
    }
  }

  private floatTo16BitPCM(float32Array: Float32Array): Int16Array {
    const int16Array = new Int16Array(float32Array.length);
    for (let i = 0; i < float32Array.length; i++) {
      const s = Math.max(-1, Math.min(1, float32Array[i]));
      int16Array[i] = s < 0 ? s * 0x8000 : s * 0x7FFF;
    }
    return int16Array;
  }

  private arrayBufferToBase64(buffer: ArrayBuffer): string {
    const bytes = new Uint8Array(buffer);
    let binary = '';
    for (let i = 0; i < bytes.byteLength; i++) {
      binary += String.fromCharCode(bytes[i]);
    }
    return btoa(binary);
  }

  private base64ToArrayBuffer(base64: string): ArrayBuffer {
    const binaryString = atob(base64);
    const bytes = new Uint8Array(binaryString.length);
    for (let i = 0; i < binaryString.length; i++) {
      bytes[i] = binaryString.charCodeAt(i);
    }
    return bytes.buffer;
  }

  private cleanup(): void {
    this.stopAudioCapture();
    this.messageSubject.complete();
    this.messageSubject = new Subject<RealtimeMessage>();
  }
}
