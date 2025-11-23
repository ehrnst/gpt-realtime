// REFERENCE IMPLEMENTATION - This shows how to update app.module.ts (if using NgModule)
// OR how to update imports in standalone components (Angular 18+)

import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { HttpClientModule } from '@angular/common/http';  // Needed for PersonaService
import { CommonModule } from '@angular/common';

import { AppComponent } from './app.component';
import { VoiceAssistantComponent } from './components/voice-assistant/voice-assistant.component';
import { PersonaSelectorComponent } from './persona-selector.component';  // ADDED

import { RealtimeService } from './services/realtime.service';
import { PersonaService } from './services/persona.service';  // ADDED

@NgModule({
  declarations: [
    AppComponent,
    VoiceAssistantComponent,
    PersonaSelectorComponent  // ADDED: Register PersonaSelectorComponent
  ],
  imports: [
    BrowserModule,
    CommonModule,
    HttpClientModule  // ADDED: Required for HTTP calls in PersonaService
  ],
  providers: [
    RealtimeService,
    PersonaService  // ADDED: Register PersonaService (though providedIn: 'root' makes this optional)
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }

// ========================================
// IF USING STANDALONE COMPONENTS (Angular 18+)
// ========================================

// In your standalone component file (e.g., voice-assistant.component.ts):
/*
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';
import { PersonaSelectorComponent } from '../../persona-selector.component';

@Component({
  selector: 'app-voice-assistant',
  standalone: true,
  imports: [
    CommonModule,
    HttpClientModule,
    PersonaSelectorComponent  // ADDED: Import PersonaSelectorComponent
  ],
  templateUrl: './voice-assistant.component.html',
  styleUrls: ['./voice-assistant.component.css']
})
export class VoiceAssistantComponent {
  // ... component code
}
*/

// In main.ts (if using standalone):
/*
import { bootstrapApplication } from '@angular/platform-browser';
import { provideHttpClient } from '@angular/common/http';
import { AppComponent } from './app/app.component';

bootstrapApplication(AppComponent, {
  providers: [
    provideHttpClient()  // ADDED: Provide HTTP client for the app
  ]
}).catch(err => console.error(err));
*/
