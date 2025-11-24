import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { PersonaSelectorComponent } from './components/persona-selector/persona-selector.component';
import { VoiceAssistantComponent } from './components/voice-assistant/voice-assistant.component';

const routes: Routes = [
  { path: '', component: PersonaSelectorComponent },
  { path: 'call', component: VoiceAssistantComponent },
  { path: '**', redirectTo: '' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
