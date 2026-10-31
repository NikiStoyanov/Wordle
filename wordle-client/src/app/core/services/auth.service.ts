import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AuthResponse,
  ChangePasswordDto,
  EmailVerificationInitiatedResponse,
  LoginDto,
  RegisterDto,
  UpdateEmailDto,
  UpdateUsernameDto,
  UserProfile,
  UserSession,
  VerifyEmailDto,
} from '../models/auth';

const TOKEN_KEY = 'wordle_jwt_token';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/auth`;

  private readonly tokenSignal = signal<string | null>(this.getStoredToken());
  readonly currentUser = signal<UserSession | null>(this.decodeToken(this.tokenSignal()));

  readonly isAuthenticated = computed(() => !!this.currentUser());
  readonly token = computed(() => this.tokenSignal());

  login(credentials: LoginDto): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.baseUrl}/login`, credentials)
      .pipe(tap((res) => this.handleAuthSuccess(res.token)));
  }

  register(credentials: RegisterDto): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.baseUrl}/register`, credentials)
      .pipe(tap((res) => this.handleAuthSuccess(res.token)));
  }

  getProfile(): Observable<UserProfile> {
    return this.http.get<UserProfile>(`${this.baseUrl}/me`);
  }

  requestEmailChange(dto: UpdateEmailDto): Observable<EmailVerificationInitiatedResponse> {
    return this.http.put<EmailVerificationInitiatedResponse>(`${this.baseUrl}/email`, dto);
  }

  verifyEmail(dto: VerifyEmailDto): Observable<UserProfile> {
    return this.http.post<UserProfile>(`${this.baseUrl}/email/verify`, dto);
  }

  updateUsername(dto: UpdateUsernameDto): Observable<AuthResponse> {
    return this.http
      .put<AuthResponse>(`${this.baseUrl}/username`, dto)
      .pipe(tap((res) => this.handleAuthSuccess(res.token)));
  }

  changePassword(dto: ChangePasswordDto): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.baseUrl}/password`, dto);
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    this.tokenSignal.set(null);
    this.currentUser.set(null);
  }

  private handleAuthSuccess(token: string): void {
    localStorage.setItem(TOKEN_KEY, token);
    this.tokenSignal.set(token);
    this.currentUser.set(this.decodeToken(token));
  }

  private getStoredToken(): string | null {
    const token = localStorage.getItem(TOKEN_KEY);
    if (!token) return null;

    const user = this.decodeToken(token);
    if (!user) {
      localStorage.removeItem(TOKEN_KEY);
      return null;
    }

    return token;
  }

  private decodeToken(token: string | null): UserSession | null {
    if (!token) return null;

    try {
      const parts = token.split('.');
      if (parts.length !== 3) return null;

      const payloadBase64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
      const decodedJson = atob(payloadBase64);
      const payload = JSON.parse(decodedJson);

      // Check expiration
      if (payload.exp && Date.now() >= payload.exp * 1000) {
        return null;
      }

      const id = Number(payload.sub ?? payload.nameid ?? 0);
      const username = payload.unique_name || payload.name || payload.sub || 'Player';

      return { id, username };
    } catch {
      return null;
    }
  }
}
