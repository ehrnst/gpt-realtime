import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface SessionToken {
  clientSecret: string;
  expiresAt: string;
  realtimeUrl: string;
  systemInstructions: string;
  voice: string;
}

export interface Persona {
  id: string;
  name: string;
  description: string;
  voice: string;
  systemInstructions: string;
  icon: string;
}

@Injectable({
  providedIn: 'root'
})
export class TokenService {
  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) { }

  getToken(personaId?: string): Observable<SessionToken> {
    const options = personaId ? { params: { personaId } } : {};
    return this.http.get<SessionToken>(`${this.apiUrl}/api/token`, options);
  }

  getPersonas(): Observable<Persona[]> {
    return this.http.get<Persona[]>(`${this.apiUrl}/api/token/personas`);
  }
}
