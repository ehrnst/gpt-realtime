import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface SessionToken {
  token: string;
  expiresAt: string;
}

@Injectable({
  providedIn: 'root'
})
export class TokenService {
  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) { }

  getToken(): Observable<SessionToken> {
    return this.http.get<SessionToken>(`${this.apiUrl}/api/token`);
  }
}
