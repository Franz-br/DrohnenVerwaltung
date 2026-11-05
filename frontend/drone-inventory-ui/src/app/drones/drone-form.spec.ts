import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Drone } from '../api/api-models';
import { DroneForm } from './drone-form';

describe('DroneForm', () => {
  let fixture: ComponentFixture<DroneForm>;
  let httpTesting: HttpTestingController;
  let page: HTMLElement;
  let created: Drone[];

  beforeEach(async () => {
    TestBed.configureTestingModule({
      imports: [DroneForm],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    httpTesting = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(DroneForm);
    created = [];
    fixture.componentInstance.created.subscribe((drone) => created.push(drone));
    await fixture.whenStable();
    page = fixture.nativeElement as HTMLElement;
  });

  afterEach(() => httpTesting.verify());

  function type(selector: string, value: string) {
    const input = page.querySelector<HTMLInputElement | HTMLTextAreaElement>(selector)!;
    input.value = value;
    input.dispatchEvent(new Event('input'));
  }

  // The submit action runs asynchronously after the HTTP response, so give it a turn to finish.
  async function settle() {
    await new Promise((resolve) => setTimeout(resolve));
    await fixture.whenStable();
  }

  async function submit() {
    page.querySelector<HTMLButtonElement>('button[type="submit"]')!.click();
    await fixture.whenStable();
  }

  it('creates a drone with name and description and resets the form', async () => {
    type('#drone-name', 'TELLO-07');
    type('#drone-description', 'SSID: TELLO-9FA21B');
    await submit();

    const request = httpTesting.expectOne('/api/drones');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ name: 'TELLO-07', description: 'SSID: TELLO-9FA21B' });
    const drone: Drone = { id: 7, name: 'TELLO-07', description: 'SSID: TELLO-9FA21B', status: 'Available' };
    request.flush(drone, { status: 201, statusText: 'Created' });
    await settle();

    expect(created).toEqual([drone]);
    expect(page.querySelector<HTMLInputElement>('#drone-name')!.value).toBe('');
    expect(page.querySelector('[role="alert"]')).toBeNull();
  });

  it('shows the backend error when the name already exists', async () => {
    type('#drone-name', 'TELLO-07');
    await submit();

    httpTesting.expectOne('/api/drones').flush(
      { title: 'Conflict', status: 409, detail: 'A drone named "TELLO-07" already exists.' },
      { status: 409, statusText: 'Conflict' }
    );
    await settle();

    expect(page.querySelector('[role="alert"]')?.textContent).toContain(
      'A drone named "TELLO-07" already exists.'
    );
    expect(created).toEqual([]);
  });

  it('does not send a drone without a name', async () => {
    await submit();

    httpTesting.expectNone('/api/drones');
    expect(page.querySelector('#drone-name-error')?.textContent).toContain('Please enter a name.');
    expect(page.querySelector('#drone-name')?.getAttribute('aria-invalid')).toBe('true');
  });
});
