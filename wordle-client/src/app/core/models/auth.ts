export interface LoginDto {
  username: string;
  password: string;
}

export interface RegisterDto {
  username: string;
  password: string;
  email?: string;
}

export interface AuthResponse {
  token: string;
  message?: string;
}

export interface UserSession {
  id: number;
  username: string;
}

export interface UserProfile {
  id: number;
  username: string;
  email?: string | null;
  pendingEmail?: string | null;
  hasPendingEmailVerification?: boolean;
  currentStreak: number;
  maxStreak: number;
  totalGamesPlayed: number;
  totalGamesWon: number;
}

export interface UpdateEmailDto {
  email: string;
}

export interface VerifyEmailDto {
  code: string;
}

export interface EmailVerificationInitiatedResponse {
  pendingEmail: string;
  message: string;
  verificationCode?: string;
}

export interface UpdateUsernameDto {
  username: string;
}

export interface ChangePasswordDto {
  currentPassword: string;
  newPassword: string;
}
