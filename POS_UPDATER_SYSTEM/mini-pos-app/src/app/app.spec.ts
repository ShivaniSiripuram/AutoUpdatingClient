import { TestBed } from '@angular/core/testing';
import { App } from './app';

// Smoke tests for the standalone root component.
describe('App', () => {
  beforeEach(async () => {
    // Importing the standalone component gives TestBed its template and styles.
    await TestBed.configureTestingModule({
      imports: [App],
    }).compileComponents();
  });

  it('should create the app', () => {
    // The component should instantiate without dependency or template errors.
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render title', async () => {
    // Rendering waits for Angular stability before querying the generated DOM.
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('Hello, mini-pos-app');
  });
});
