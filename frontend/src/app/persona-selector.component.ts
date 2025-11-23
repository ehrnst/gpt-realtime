import { Component, OnInit, Output, EventEmitter } from '@angular/core';
import { PersonaService } from './services/persona.service';
import { Persona } from './services/persona.interface';

@Component({
  selector: 'app-persona-selector',
  templateUrl: './persona-selector.component.html',
  styleUrls: ['./persona-selector.component.css']
})
export class PersonaSelectorComponent implements OnInit {
  personas: Persona[] = [];
  selectedPersona: Persona | null = null;
  loading = true;
  error: string | null = null;

  @Output() personaSelected = new EventEmitter<Persona>();

  constructor(private personaService: PersonaService) {}

  ngOnInit(): void {
    this.loadPersonas();
  }

  loadPersonas(): void {
    this.loading = true;
    this.error = null;
    
    this.personaService.getPersonas().subscribe({
      next: (personas) => {
        this.personas = personas;
        this.loading = false;
        
        // Auto-select first persona if available
        if (personas.length > 0 && !this.selectedPersona) {
          this.selectPersona(personas[0]);
        }
      },
      error: (err) => {
        this.error = 'Failed to load personas. Please try again.';
        this.loading = false;
        console.error('Error loading personas:', err);
      }
    });
  }

  selectPersona(persona: Persona): void {
    this.selectedPersona = persona;
    this.personaSelected.emit(persona);
  }

  isSelected(persona: Persona): boolean {
    return this.selectedPersona?.id === persona.id;
  }
}
