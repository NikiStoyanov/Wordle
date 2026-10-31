import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { AuthService } from './auth.service';
import { environment } from '../../../environments/environment';

describe('AuthService', () => {
  let service: AuthService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AuthService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
    localStorage.clear();
  });

  it('should be created and start unauthenticated', () => {
    expect(service).toBeTruthy();
    expect(service.isAuthenticated()).toBe(false);
    expect(service.currentUser()).toBeNull();
  });

  it('should login and set current user from JWT', () => {
    const fakePayload = btoa(
      JSON.stringify({ sub: '42', unique_name: 'player1', exp: 9999999999 })
    );
    const fakeToken = `header.${fakePayload}.signature`;

    service
      .login({ username: 'player1', password: 'password123' })
      .subscribe((res) => {
        expect(res.token).toBe(fakeToken);
        expect(service.isAuthenticated()).toBe(true);
        expect(service.currentUser()?.id).toBe(42);
        expect(service.currentUser()?.username).toBe('player1');
      });

    const req = httpTesting.expectOne(`${environment.apiUrl}/auth/login`);
    expect(req.request.method).toBe('POST');
    req.flush({ token: fakeToken });
  });

  it('should logout and clear state', () => {
    const fakePayload = btoa(
      JSON.stringify({ sub: '1', unique_name: 'alice', exp: 9999999999 })
    );
    const fakeToken = `header.${fakePayload}.signature`;

    service.login({ username: 'alice', password: 'password' }).subscribe();
    const req = httpTesting.expectOne(`${environment.apiUrl}/auth/login`);
    req.flush({ token: fakeToken });

    expect(service.isAuthenticated()).toBe(true);

    service.logout();
    expect(service.isAuthenticated()).toBe(false);
    expect(service.currentUser()).toBeNull();
    expect(service.token()).toBeNull();
  });
});
