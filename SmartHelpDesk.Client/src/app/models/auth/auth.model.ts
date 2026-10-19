
export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthResponse {
  userId: string;
  fullName: string;
  email: string;
  accessToken: string;
  refreshToken: string;
  expiresAtUtc: string;
  profileImageUrl: string | null;
}

export interface RegisterRequest {
  fullName: string;
  email: string;
  password: string;
  profileImage?: File | null;
}

export interface ApiError {
  message?: string;
  errors?: string[];
}
