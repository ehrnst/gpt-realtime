import { Component, OnInit, OnDestroy, ViewChild, ElementRef } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom, Subscription } from 'rxjs';
import { TokenService, Persona } from '../../services/token.service';
import { RealtimeService, RealtimeMessage } from '../../services/realtime.service';

@Component({
  selector: 'app-voice-assistant',
  templateUrl: './voice-assistant.component.html',
  styleUrl: './voice-assistant.component.css'
})
export class VoiceAssistantComponent implements OnInit, OnDestroy {
  isConnected = false;
  isConnecting = false;
  isAudioCaptureEnabled = false;
  statusMessage = 'Ready to call';
  messages: string[] = [];
  selectedPersona?: Persona;
  private messageSubscription?: Subscription;
  @ViewChild('remoteAudio') remoteAudioRef?: ElementRef<HTMLAudioElement>;
  @ViewChild('messagesContainer') messagesContainer?: ElementRef<HTMLDivElement>;

  constructor(
    private tokenService: TokenService,
    private realtimeService: RealtimeService,
    private router: Router
  ) {
    // Get persona from router state
    const navigation = this.router.getCurrentNavigation();
    if (navigation?.extras?.state) {
      this.selectedPersona = navigation.extras.state['persona'];
    }
  }

  ngOnInit(): void {}

  ngOnDestroy(): void {
    this.hangUp();
  }

  getStatusClass(): string {
    if (this.isConnected) return 'status-connected';
    if (this.isConnecting) return 'status-connecting';
    return 'status-ready';
  }

  getTimestamp(): string {
    return new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  }

  trackByMessage(index: number, message: string): number {
    return index;
  }

  async startCall(): Promise<void> {
    if (!this.remoteAudioRef) {
      this.statusMessage = 'Audio output not ready';
      return;
    }

    this.isConnecting = true;
    this.statusMessage = 'Calling...';

    try {
      const tokenData = await firstValueFrom(this.tokenService.getToken(this.selectedPersona?.id));
      const messages$ = await this.realtimeService.connect(
        tokenData.clientSecret,
        tokenData.realtimeUrl,
        this.remoteAudioRef.nativeElement,
        tokenData.systemInstructions,
        tokenData.voice
      );

      this.messageSubscription = messages$.subscribe({
        next: (message) => this.handleRealtimeMessage(message),
        error: (error) => {
          console.error('Realtime service error:', error);
          this.statusMessage = 'Call failed: ' + (error?.message ?? 'Unknown');
          this.isConnected = false;
          this.isConnecting = false;
        }
      });

      // Automatically start audio capture once connected
      this.addMessage('Call connected - assistant will greet you first');
    } catch (error: any) {
      console.error('Error connecting to realtime session:', error);
      this.statusMessage = 'Call failed: ' + (error?.message ?? 'Unknown');
      this.isConnecting = false;
    }
  }

  goBack(): void {
    this.router.navigate(['/']);
  }

  hangUp(): void {
    this.realtimeService.disconnect();
    this.messageSubscription?.unsubscribe();
    this.isConnected = false;
    this.isConnecting = false;
    this.isAudioCaptureEnabled = false;
    this.statusMessage = 'Call ended';
    this.addMessage('Call disconnected');
  }

  private async enableMicrophone(): Promise<void> {
    try {
      await this.realtimeService.startAudioCapture();
      this.isAudioCaptureEnabled = true;
      this.addMessage('Microphone enabled - speak freely');
    } catch (error: any) {
      console.error('Error enabling microphone:', error);
      this.addMessage('Error enabling microphone: ' + (error?.message ?? 'Unknown'));
    }
  }

  private handleRealtimeMessage(message: RealtimeMessage): void {
    console.log('Received message:', message);
    
    switch (message.type) {
      case 'session.created':
        this.addMessage('Session created successfully');
        this.statusMessage = 'Assistant is answering...';
        this.isConnected = true;
        this.isConnecting = false;
        // Don't automatically start audio capture yet - let assistant speak first
        break;
      case 'session.updated':
        this.addMessage('Session updated');
        break;
      case 'response.output_item.added':
        this.addMessage('Assistant is responding...');
        break;
      case 'response.output_item.done':
        this.addMessage('Assistant response complete');
        break;
      case 'response.audio.delta':
        // Audio data received - we don't need to log each chunk
        break;
      case 'response.audio.done':
        this.addMessage('Audio response finished');
        break;
      case 'response.text.delta':
        if (message['delta']) {
          this.addMessage('Assistant: ' + message['delta']);
        }
        break;
      case 'response.done':
        this.addMessage('Response completed');
        // Enable microphone after assistant finishes speaking
        if (this.isConnected && !this.isAudioCaptureEnabled) {
          this.enableMicrophone();
          this.statusMessage = 'Assistant finished - you can speak now';
        }
        break;
      case 'input_audio_buffer.speech_started':
        this.addMessage('Speech detected');
        break;
      case 'input_audio_buffer.speech_stopped':
        this.addMessage('Speech ended');
        break;
      case 'peer.data_channel':
        this.addMessage(`Data channel ${message['state']}`);
        if (message['state'] === 'open') {
          this.statusMessage = 'Call initializing...';
        }
        break;
      case 'peer.ice_state':
        this.addMessage(`ICE connection ${message['state']}`);
        if (message['state'] === 'connected') {
          this.statusMessage = 'WebRTC connected - establishing session...';
        } else if (message['state'] === 'disconnected' || message['state'] === 'failed') {
          this.statusMessage = 'Call disconnected';
          this.isConnected = false;
          this.isConnecting = false;
        }
        break;
      case 'error':
        this.addMessage('Error: ' + (message['error']?.['message'] || 'Unknown error'));
        this.statusMessage = 'Error occurred';
        break;
      default:
        console.debug('Unhandled realtime event:', message.type, message);
    }
  }

  private addMessage(message: string): void {
    this.messages.push(`[${new Date().toLocaleTimeString()}] ${message}`);
    // Keep only last 50 messages
    if (this.messages.length > 50) {
      this.messages.shift();
    }
    
    // Auto-scroll to bottom
    setTimeout(() => {
      if (this.messagesContainer) {
        const element = this.messagesContainer.nativeElement;
        element.scrollTop = element.scrollHeight;
      }
    }, 100);
  }
}
