import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { Drone } from '../api/api-models';
import { DroneForm } from './drone-form';
import { DroneListPage } from './drone-list-page';

describe('DroneListPage', () => {
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [DroneListPage],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  async function render(response: Drone[] | 'error') {
    const fixture = TestBed.createComponent(DroneListPage);
    const request = httpTesting.expectOne('/api/drones');
    expect(request.request.method).toBe('GET');
    if (response === 'error') {
      request.flush(null, { status: 500, statusText: 'Server Error' });
    } else {
      request.flush(response);
    }
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it('lists drones with name, status and description', async () => {
    const page = await render([
      { id: 2, name: 'TELLO-02', description: 'reserve', status: 'Available' },
      {
        id: 7,
        name: 'TELLO-07',
        description: 'SSID: TELLO-9FA21B, rechter Propellerschutz beschädigt',
        status: 'Loaned'
      }
    ]);

    const rows = Array.from(page.querySelectorAll('tbody tr')).map((row) =>
      Array.from(row.querySelectorAll('td')).map((cell) => cell.textContent?.trim())
    );
    expect(rows).toEqual([
      ['TELLO-02', 'Available', 'reserve'],
      ['TELLO-07', 'Loaned', 'SSID: TELLO-9FA21B, rechter Propellerschutz beschädigt']
    ]);
  });

  it('shows a message when there are no drones', async () => {
    const page = await render([]);

    expect(page.textContent).toContain('No drones yet.');
    expect(page.querySelector('table')).toBeNull();
  });

  it('shows an error when the drones cannot be loaded', async () => {
    const page = await render('error');

    expect(page.querySelector('[role="alert"]')?.textContent).toContain('could not be loaded');
  });

  it('reloads the list after a drone was created', async () => {
    const fixture = TestBed.createComponent(DroneListPage);
    httpTesting.expectOne('/api/drones').flush([]);
    await fixture.whenStable();

    const tello07: Drone = { id: 7, name: 'TELLO-07', description: '', status: 'Available' };
    fixture.debugElement.query(By.directive(DroneForm)).componentInstance.created.emit(tello07);
    httpTesting.expectOne('/api/drones').flush([tello07]);
    await fixture.whenStable();

    expect((fixture.nativeElement as HTMLElement).querySelector('tbody td')?.textContent).toBe('TELLO-07');
  });
});
