import { Component, OnInit, OnDestroy } from '@angular/core';
import { TokenService } from '../../services/token.service';
import { RealtimeService, RealtimeMessage } from '../../services/realtime.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-voice-assistant',
  templateUrl: './voice-assistant.component.html',
  styleUrl: './voice-assistant.component.css'
})
export class VoiceAssistantComponent implements OnInit, OnDestroy {
  isConnected = false;
  isRecording = false;
  statusMessage = 'Click "Connect" to start';
  messages: string[] = [];
  private messageSubscription?: Subscription;

  constructor(
    private tokenService: TokenService,
    private realtimeService: RealtimeService
  ) {}

  ngOnInit(): void {}

  ngOnDestroy(): void {
    this.disconnect();
  }

  connect(): void {
    this.statusMessage = 'Connecting...';
    this.tokenService.getToken().subscribe({
      next: (tokenData) => {
        this.messageSubscription = this.realtimeService.connect(tokenData.token).subscribe({
          next: (message) => this.handleRealtimeMessage(message),
          error: (error) => {
            console.error('Realtime service error:', error);
            this.statusMessage = 'Error: ' + error.message;
            this.isConnected = false;
          }
        });
        this.isConnected = true;
        this.statusMessage = 'Connected! Click "Start Recording" to begin';
      },
      error: (error) => {
        console.error('Error getting token:', error);
        this.statusMessage = 'Error getting token: ' + error.message;
      }
    });
  }

  disconnect(): void {
    if (this.isRecording) {
      this.stopRecording();
    }
    this.realtimeService.disconnect();
    this.messageSubscription?.unsubscribe();
    this.isConnected = false;
    this.statusMessage = 'Disconnected';
  }

  async startRecording(): Promise<void> {
    try {
      await this.realtimeService.startAudioCapture();
      this.isRecording = true;
      this.statusMessage = 'Recording... Speak now';
      this.addMessage('Started recording');
    } catch (error: any) {
      console.error('Error starting recording:', error);
      this.statusMessage = 'Error starting recording: ' + error.message;
    }
  }

  stopRecording(): void {
    this.realtimeService.stopAudioCapture();
    this.realtimeService.sendMessage({ type: 'input_audio_buffer.commit' });
    this.isRecording = false;
    this.statusMessage = 'Stopped recording';
    this.addMessage('Stopped recording');
  }

  private handleRealtimeMessage(message: RealtimeMessage): void {
    console.log('Received message:', message);
    
    switch (message.type) {
      case 'session.created':
        this.addMessage('Session created');
        break;
      case 'session.updated':
        this.addMessage('Session configured');
        break;
      case 'conversation.item.created':
        if (message['item']?.['content']) {
          this.addMessage('Assistant: ' + JSON.stringify(message['item']['content']));
        }
        break;
      case 'response.done':
        this.addMessage('Response completed');
        break;
      case 'error':
        this.addMessage('Error: ' + message['error']?.['message']);
        break;
    }
  }

  private addMessage(message: string): void {
    this.messages.push(`[${new Date().toLocaleTimeString()}] ${message}`);
    // Keep only last 50 messages
    if (this.messages.length > 50) {
      this.messages.shift();
    }
  }
}
