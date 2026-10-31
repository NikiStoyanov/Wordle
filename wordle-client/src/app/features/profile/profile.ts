import { Component, OnInit, inject, signal } from '@angular/core';
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators,
  AbstractControl,
  ValidationErrors,
  ValidatorFn,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { UserProfile } from '../../core/models/auth';

export const newPasswordsMatchValidator: ValidatorFn = (
  control: AbstractControl
): ValidationErrors | null => {
  const newPassword = control.get('newPassword')?.value;
  const confirmPassword = control.get('confirmPassword')?.value;
  return newPassword && confirmPassword && newPassword !== confirmPassword
    ? { passwordsMismatch: true }
    : null;
};

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './profile.html',
  styleUrl: './profile.css',
})
export class Profile implements OnInit {
  private readonly fb = inject(FormBuilder);
  readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly profile = signal<UserProfile | null>(null);
  readonly isLoading = signal(false);

  // Status messages for each section
  readonly usernameSuccess = signal<string | null>(null);
  readonly usernameError = signal<string | null>(null);
  readonly isUpdatingUsername = signal(false);

  readonly emailSuccess = signal<string | null>(null);
  readonly emailError = signal<string | null>(null);
  readonly isRequestingEmail = signal(false);

  // Email verification state
  readonly pendingEmail = signal<string | null>(null);
  readonly verificationCodeInfo = signal<string | null>(null);
  readonly isVerifyingEmail = signal(false);
  readonly verifySuccess = signal<string | null>(null);
  readonly verifyError = signal<string | null>(null);

  readonly passwordSuccess = signal<string | null>(null);
  readonly passwordError = signal<string | null>(null);
  readonly isChangingPassword = signal(false);

  readonly usernameForm = this.fb.nonNullable.group({
    username: [
      '',
      [Validators.required, Validators.minLength(3), Validators.maxLength(30)],
    ],
  });

  readonly emailForm = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
  });

  readonly verifyCodeForm = this.fb.nonNullable.group({
    code: ['', [Validators.required, Validators.minLength(6), Validators.maxLength(6)]],
  });

  readonly passwordForm = this.fb.nonNullable.group(
    {
      currentPassword: ['', [Validators.required]],
      newPassword: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', [Validators.required]],
    },
    { validators: [newPasswordsMatchValidator] }
  );

  ngOnInit(): void {
    this.loadProfile();
  }

  loadProfile(): void {
    this.isLoading.set(true);
    this.authService.getProfile().subscribe({
      next: (data: UserProfile) => {
        this.isLoading.set(false);
        this.profile.set(data);
        this.usernameForm.patchValue({ username: data.username });
        if (data.email) {
          this.emailForm.patchValue({ email: data.email });
        }
        if (data.pendingEmail) {
          this.pendingEmail.set(data.pendingEmail);
        }
      },
      error: () => {
        this.isLoading.set(false);
      },
    });
  }

  onUpdateUsername(): void {
    if (this.usernameForm.invalid) {
      this.usernameForm.markAllAsTouched();
      return;
    }

    this.isUpdatingUsername.set(true);
    this.usernameSuccess.set(null);
    this.usernameError.set(null);

    const { username } = this.usernameForm.getRawValue();

    this.authService.updateUsername({ username: username.trim() }).subscribe({
      next: () => {
        this.isUpdatingUsername.set(false);
        this.usernameSuccess.set('Username successfully updated!');
        if (this.profile()) {
          this.profile.update((p) => (p ? { ...p, username: username.trim() } : null));
        }
      },
      error: (err: any) => {
        this.isUpdatingUsername.set(false);
        const msg =
          err?.error?.message || err?.error?.Message || 'Failed to update username.';
        this.usernameError.set(msg);
      },
    });
  }

  onRequestEmailChange(): void {
    if (this.emailForm.invalid) {
      this.emailForm.markAllAsTouched();
      return;
    }

    this.isRequestingEmail.set(true);
    this.emailSuccess.set(null);
    this.emailError.set(null);
    this.verifySuccess.set(null);
    this.verifyError.set(null);

    const { email } = this.emailForm.getRawValue();

    this.authService.requestEmailChange({ email: email.trim() }).subscribe({
      next: (res) => {
        this.isRequestingEmail.set(false);
        this.pendingEmail.set(res.pendingEmail);
        this.emailSuccess.set(`Verification code generated for ${res.pendingEmail}.`);
        if (res.verificationCode) {
          this.verificationCodeInfo.set(res.verificationCode);
        }
      },
      error: (err: any) => {
        this.isRequestingEmail.set(false);
        const msg =
          err?.error?.message || err?.error?.Message || 'Failed to request email change.';
        this.emailError.set(msg);
      },
    });
  }

  onVerifyEmail(): void {
    if (this.verifyCodeForm.invalid) {
      this.verifyCodeForm.markAllAsTouched();
      return;
    }

    this.isVerifyingEmail.set(true);
    this.verifySuccess.set(null);
    this.verifyError.set(null);

    const { code } = this.verifyCodeForm.getRawValue();

    this.authService.verifyEmail({ code: code.trim() }).subscribe({
      next: (updatedProfile: UserProfile) => {
        this.isVerifyingEmail.set(false);
        this.verifySuccess.set('Email successfully verified and assigned!');
        this.profile.set(updatedProfile);
        this.pendingEmail.set(null);
        this.verificationCodeInfo.set(null);
        this.verifyCodeForm.reset();
      },
      error: (err: any) => {
        this.isVerifyingEmail.set(false);
        const msg =
          err?.error?.message || err?.error?.Message || 'Verification failed.';
        this.verifyError.set(msg);
      },
    });
  }

  onChangePassword(): void {
    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      return;
    }

    this.isChangingPassword.set(true);
    this.passwordSuccess.set(null);
    this.passwordError.set(null);

    const { currentPassword, newPassword } = this.passwordForm.getRawValue();

    this.authService.changePassword({ currentPassword, newPassword }).subscribe({
      next: () => {
        this.isChangingPassword.set(false);
        this.passwordSuccess.set('Password changed successfully!');
        this.passwordForm.reset();
      },
      error: (err: any) => {
        this.isChangingPassword.set(false);
        const msg =
          err?.error?.message || err?.error?.Message || 'Failed to change password.';
        this.passwordError.set(msg);
      },
    });
  }

  onLogout(): void {
    this.authService.logout();
    this.router.navigate(['/']);
  }
}
