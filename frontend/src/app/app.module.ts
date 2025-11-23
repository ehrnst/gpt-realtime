import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { HttpClientModule } from '@angular/common/http';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { VoiceAssistantComponent } from './components/voice-assistant/voice-assistant.component';
import { PersonaSelectorComponent } from './components/persona-selector/persona-selector.component';

@NgModule({
  declarations: [
    AppComponent,
    VoiceAssistantComponent,
    PersonaSelectorComponent
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    HttpClientModule
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
