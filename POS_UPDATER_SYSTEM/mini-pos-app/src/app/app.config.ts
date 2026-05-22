import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';

import { routes } from './app.routes';

// Central Angular application configuration for standalone bootstrapping.
export const appConfig: ApplicationConfig = {
  providers: [
    // Forward browser-level errors into Angular's global error handling.
    provideBrowserGlobalErrorListeners(),
    // Register app routes, even though this simple POS screen has no child pages yet.
    provideRouter(routes)
  ]
};
