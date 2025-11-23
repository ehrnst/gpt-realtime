import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { TokenService, Persona } from '../../services/token.service';

@Component({
  selector: 'app-persona-selector',
  templateUrl: './persona-selector.component.html',
  styleUrl: './persona-selector.component.css'
})
export class PersonaSelectorComponent implements OnInit {
  personas: Persona[] = [];
  isLoading = true;
  errorMessage = '';

  constructor(
    private tokenService: TokenService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadPersonas();
  }

  loadPersonas(): void {
    this.isLoading = true;
    this.tokenService.getPersonas().subscribe({
      next: (personas) => {
        this.personas = personas;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading personas:', error);
        this.errorMessage = 'Failed to load personas. Please try again.';
        this.isLoading = false;
      }
    });
  }

  selectPersona(persona: Persona): void {
    this.router.navigate(['/call'], { 
      state: { persona } 
    });
  }
}
