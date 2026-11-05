import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Drone, Loan, StudentGroup } from '../api/api-models';
import { LoanFormPage } from './loan-form-page';

const drones: Drone[] = [
  { id: 2, name: 'TELLO-02', description: '', status: 'Loaned' },
  { id: 7, name: 'TELLO-07', description: '', status: 'Available' },
  { id: 9, name: 'TELLO-09', description: '', status: 'Available' }
];
const groups: StudentGroup[] = [
  { id: 1, name: 'Group 1' },
  { id: 3, name: 'Group 3' }
];

describe('LoanFormPage', () => {
  let fixture: ComponentFixture<LoanFormPage>;
  let httpTesting: HttpTestingController;
  let page: HTMLElement;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      imports: [LoanFormPage],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    httpTesting = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(LoanFormPage);
    page = fixture.nativeElement as HTMLElement;
    flushOptions();
    await fixture.whenStable();
  });

  afterEach(() => httpTesting.verify());

  function flushOptions() {
    httpTesting.expectOne('/api/drones').flush(drones);
    httpTesting.expectOne('/api/student-groups').flush(groups);
  }

  function setValue(selector: string, value: string) {
    const control = page.querySelector<HTMLInputElement | HTMLSelectElement>(selector)!;
    control.value = value;
    control.dispatchEvent(new Event('input'));
    control.dispatchEvent(new Event('change'));
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

  async function fillTello07ForGroup3() {
    setValue('#loan-drone', '7');
    setValue('#loan-group', '3');
    setValue('#loan-batteries', '4');
    setValue('#loan-propellers', '8');
    await fixture.whenStable();
  }

  it('offers only available drones', () => {
    const options = Array.from(page.querySelectorAll('#loan-drone option')).map((o) => o.textContent?.trim());

    expect(options).toEqual(['Choose an available drone', 'TELLO-07', 'TELLO-09']);
  });

  it('loans TELLO-07 to Group 3 with 4 batteries and 8 propellers', async () => {
    await fillTello07ForGroup3();
    await submit();

    const request = httpTesting.expectOne({ method: 'POST', url: '/api/loans' });
    expect(request.request.body).toEqual({
      droneId: 7,
      studentGroupId: 3,
      items: [
        { type: 'Battery', quantity: 4 },
        { type: 'Propeller', quantity: 8 }
      ]
    });
    const loan: Loan = {
      id: 1,
      droneId: 7,
      droneName: 'TELLO-07',
      studentGroupId: 3,
      studentGroupName: 'Group 3',
      loanDate: '2026-08-19T08:00:00Z',
      returnDate: null,
      status: 'Active',
      batteries: 4,
      propellers: 8
    };
    request.flush(loan, { status: 201, statusText: 'Created' });
    await settle();

    expect(page.querySelector('[role="status"]')?.textContent).toContain('TELLO-07 was loaned to Group 3.');
    flushOptions();
    await fixture.whenStable();
    expect(page.querySelector<HTMLSelectElement>('#loan-drone')!.value).toBe('');
  });

  it('shows the backend message when the drone already has an active loan', async () => {
    await fillTello07ForGroup3();
    await submit();

    httpTesting.expectOne({ method: 'POST', url: '/api/loans' }).flush(
      { title: 'Conflict', status: 409, detail: 'The drone "TELLO-07" already has an active loan.' },
      { status: 409, statusText: 'Conflict' }
    );
    await settle();

    expect(page.querySelector('[role="alert"]')?.textContent).toContain(
      'The drone "TELLO-07" already has an active loan.'
    );
  });

  it('does not send a loan without drone and student group', async () => {
    await submit();

    httpTesting.expectNone({ method: 'POST', url: '/api/loans' });
    expect(page.querySelector('#loan-drone-error')?.textContent).toContain('Please choose a drone.');
    expect(page.querySelector('#loan-group-error')?.textContent).toContain('Please choose a student group.');
  });

  it('does not send a negative quantity', async () => {
    await fillTello07ForGroup3();
    setValue('#loan-batteries', '-1');
    await fixture.whenStable();
    await submit();

    httpTesting.expectNone({ method: 'POST', url: '/api/loans' });
    expect(page.querySelector('#loan-batteries-error')?.textContent).toContain('Batteries must not be negative.');
  });
});
