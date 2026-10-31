import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { Profile } from './profile';
import { AuthService } from '../../core/services/auth.service';
import { UserProfile } from '../../core/models/auth';

describe('Profile Component', () => {
  let component: Profile;
  let fixture: ComponentFixture<Profile>;
  let authService: AuthService;

  const mockProfile: UserProfile = {
    id: 1,
    username: 'testplayer',
    email: 'player@example.com',
    currentStreak: 5,
    maxStreak: 12,
    totalGamesPlayed: 25,
    totalGamesWon: 20,
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Profile],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    }).compileComponents();

    authService = TestBed.inject(AuthService);
    vi.spyOn(authService, 'getProfile').mockReturnValue(of(mockProfile));

    fixture = TestBed.createComponent(Profile);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create and load profile data into forms', () => {
    expect(component).toBeTruthy();
    expect(component.profile()?.username).toBe('testplayer');
    expect(component.usernameForm.value.username).toBe('testplayer');
    expect(component.emailForm.value.email).toBe('player@example.com');
  });

  it('should validate email format in emailForm', () => {
    component.emailForm.controls.email.setValue('not-an-email');
    expect(component.emailForm.valid).toBe(false);

    component.emailForm.controls.email.setValue('valid@domain.com');
    expect(component.emailForm.valid).toBe(true);
  });

  it('should request email change and expose pending email with verification section', () => {
    vi.spyOn(authService, 'requestEmailChange').mockReturnValue(
      of({
        pendingEmail: 'new@example.com',
        message: 'Verification code generated',
        verificationCode: '654321',
      })
    );

    component.emailForm.controls.email.setValue('new@example.com');
    component.onRequestEmailChange();

    expect(component.pendingEmail()).toBe('new@example.com');
    expect(component.verificationCodeInfo()).toBe('654321');
    expect(component.emailSuccess()).toContain('Verification code generated');
  });

  it('should reject email verification if code is invalid length', () => {
    component.pendingEmail.set('new@example.com');
    component.verifyCodeForm.controls.code.setValue('123'); // < 6 digits
    expect(component.verifyCodeForm.valid).toBe(false);

    component.verifyCodeForm.controls.code.setValue('123456');
    expect(component.verifyCodeForm.valid).toBe(true);
  });

  it('should verify email and assign verified email to profile', () => {
    const verifiedProfile: UserProfile = {
      ...mockProfile,
      email: 'new@example.com',
      pendingEmail: null,
    };
    vi.spyOn(authService, 'verifyEmail').mockReturnValue(of(verifiedProfile));

    component.pendingEmail.set('new@example.com');
    component.verifyCodeForm.controls.code.setValue('123456');
    component.onVerifyEmail();

    expect(component.verifySuccess()).toBe('Email successfully verified and assigned!');
    expect(component.profile()?.email).toBe('new@example.com');
    expect(component.pendingEmail()).toBeNull();
  });

  it('should display error if email is already taken by another profile', () => {
    vi.spyOn(authService, 'requestEmailChange').mockReturnValue(
      throwError(() => ({
        error: { message: 'This email is already used by another profile.' },
      }))
    );

    component.emailForm.controls.email.setValue('taken@example.com');
    component.onRequestEmailChange();

    expect(component.emailError()).toBe('This email is already used by another profile.');
  });
});
