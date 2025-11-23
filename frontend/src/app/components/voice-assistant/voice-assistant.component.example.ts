// REFERENCE IMPLEMENTATION - This file shows key changes needed in voice-assistant.component.ts
// Main changes: Add persona selection logic and update connect() method

import { Component, OnInit, OnDestroy } from '@angular/core';
import { RealtimeService } from '../../services/realtime.service';
import { Persona } from '../../services/persona.interface';  // ADDED
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-voice-assistant',
  templateUrl: './voice-assistant.component.html',
  styleUrls: ['./voice-assistant.component.css']
})
export class VoiceAssistantComponent implements OnInit, OnDestroy {
  // ADDED: Persona-related properties
  selectedPersona: Persona | null = null;
  showPersonaSelector = true;
  
  // Existing properties
  statusMessage = 'Select an assistant to get started';
  isConnected = false;
  isRecording = false;
  messages: any[] = [];
  
  private subscriptions: Subscription[] = [];

  constructor(private realtimeService: RealtimeService) {}

  ngOnInit(): void {
    // Subscribe to connection state
    this.subscriptions.push(
      this.realtimeService.connectionState$.subscribe(state => {
        this.handleConnectionState(state);
      })
    );

    // Subscribe to messages
    this.subscriptions.push(
      this.realtimeService.messages$.subscribe(message => {
        this.handleMessage(message);
      })
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.forEach(sub => sub.unsubscribe());
    this.disconnect();
  }

  // ADDED: Handle persona selection
  onPersonaSelected(persona: Persona): void {
    this.selectedPersona = persona;
    this.showPersonaSelector = false;
    this.connect();
  }

  // UPDATED: Connect method now uses selected persona
  async connect(): Promise<void> {
    if (!this.selectedPersona) {
      console.error('No persona selected');
      return;
    }

    try {
      this.statusMessage = `Connecting to ${this.selectedPersona.name}...`;
      
      // UPDATED: Pass persona ID to connect method
      await this.realtimeService.connect(this.selectedPersona.id);
      
      this.isConnected = true;
      this.statusMessage = `Connected to ${this.selectedPersona.name}! Listening for greeting...`;
    } catch (error) {
      this.statusMessage = 'Connection failed. Please try again.';
      this.isConnected = false;
      this.showPersonaSelector = true;
      console.error('Connection error:', error);
    }
  }

  // ADDED: Method to change persona
  changePersona(): void {
    this.disconnect();
    this.showPersonaSelector = true;
    this.selectedPersona = null;
    this.statusMessage = 'Select an assistant to get started';
  }

  disconnect(): void {
    this.realtimeService.disconnect();
    this.isConnected = false;
    this.isRecording = false;
    this.statusMessage = 'Disconnected';
  }

  startRecording(): void {
    if (!this.isConnected) return;
    
    this.realtimeService.startAudioCapture();
    this.isRecording = true;
    this.statusMessage = 'Listening... Click to stop';
  }

  stopRecording(): void {
    if (!this.isConnected) return;
    
    this.realtimeService.stopAudioCapture();
    this.isRecording = false;
    this.statusMessage = 'Processing... Waiting for response';
  }

  private handleConnectionState(state: string): void {
    switch (state) {
      case 'connecting':
        this.statusMessage = 'Connecting...';
        break;
      case 'connected':
        this.isConnected = true;
        break;
      case 'disconnected':
        this.isConnected = false;
        this.statusMessage = 'Disconnected';
        break;
      case 'failed':
        this.isConnected = false;
        this.statusMessage = 'Connection failed';
        break;
    }
  }

  private handleMessage(message: any): void {
    console.log('Received message:', message);
    this.messages.push(message);

    switch (message.type) {
      case 'session.created':
        this.statusMessage = 'Session created. Waiting for greeting...';
        break;
      
      case 'response.audio.delta':
        // Handle audio response
        break;
      
      case 'response.done':
        this.statusMessage = 'Ready! Click to talk';
        break;
      
      case 'input_audio_buffer.speech_started':
        this.statusMessage = 'Listening...';
        break;
      
      case 'input_audio_buffer.speech_stopped':
        this.statusMessage = 'Processing...';
        break;
    }
  }
}
