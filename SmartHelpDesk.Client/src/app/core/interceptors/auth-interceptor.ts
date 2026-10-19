
import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';

import { Auth } from '../services/auth';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(Auth);
  const token = auth.getAccessToken();

  // Public authentication endpoints do not require a bearer token.
  const isPublicAuthEndpoint =
    /\/Auth\/(login|register|refresh)$/i.test(request.url);

  if (!token || isPublicAuthEndpoint) {
    return next(request);
  }

  const authenticatedRequest = request.clone({
    setHeaders: {
      Authorization: `Bearer ${token}`
    }
  });

  return next(authenticatedRequest);
};
