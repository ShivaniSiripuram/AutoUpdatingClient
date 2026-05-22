import { Component, signal } from '@angular/core';


// Root component for the Mini POS status screen.
@Component({
  selector: 'app-root',
  imports: [],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  // Signal-backed title kept for future template or metadata use.
  protected readonly title = signal('mini-pos-app');
}
