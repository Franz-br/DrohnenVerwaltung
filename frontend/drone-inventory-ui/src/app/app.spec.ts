import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { App } from './app';
import { routes } from './app.routes';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      // The pages load data; the testing backend keeps those requests inside the test.
      providers: [provideRouter(routes), provideHttpClient(), provideHttpClientTesting()]
    }).compileComponents();
  });

  it('should render the navigation to every page', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const links = Array.from(
      (fixture.nativeElement as HTMLElement).querySelectorAll<HTMLAnchorElement>('nav a')
    );

    expect(links.map((link) => [link.textContent?.trim(), link.getAttribute('href')])).toEqual([
      ['Drones', '/drones'],
      ['Student groups', '/student-groups'],
      ['Loan a drone', '/loans/new'],
      ['Active loans', '/loans'],
      ['Loan history', '/loans/history']
    ]);
  });

  it('should redirect the start page to the drones page', async () => {
    const harness = await RouterTestingHarness.create('/');

    expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toBe('Drones');
  });

  it.each([
    ['/student-groups', 'Student groups'],
    ['/loans/new', 'Loan a drone'],
    ['/loans', 'Active loans'],
    ['/loans/history', 'Loan history']
  ])('should lazy load the page for %s', async (url, heading) => {
    const harness = await RouterTestingHarness.create(url);

    expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toBe(heading);
  });
});
