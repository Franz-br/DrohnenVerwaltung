import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { StudentGroup } from '../api/api-models';
import { StudentGroupsPage } from './student-groups-page';

describe('StudentGroupsPage', () => {
  let fixture: ComponentFixture<StudentGroupsPage>;
  let httpTesting: HttpTestingController;
  let page: HTMLElement;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [StudentGroupsPage],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    httpTesting = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(StudentGroupsPage);
    page = fixture.nativeElement as HTMLElement;
  });

  afterEach(() => httpTesting.verify());

  function groupNames() {
    return Array.from(page.querySelectorAll('.group-list li')).map((item) => item.textContent?.trim());
  }

  function type(value: string) {
    const input = page.querySelector<HTMLInputElement>('#group-name')!;
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

  it('lists the student groups', async () => {
    httpTesting.expectOne('/api/student-groups').flush([
      { id: 1, name: 'Group 1' },
      { id: 3, name: 'Group 3' }
    ] satisfies StudentGroup[]);
    await fixture.whenStable();

    expect(groupNames()).toEqual(['Group 1', 'Group 3']);
  });

  it('creates a student group and reloads the list', async () => {
    httpTesting.expectOne('/api/student-groups').flush([]);
    await fixture.whenStable();

    type('Group 3');
    await submit();
    const request = httpTesting.expectOne({ method: 'POST', url: '/api/student-groups' });
    expect(request.request.body).toEqual({ name: 'Group 3' });
    request.flush({ id: 3, name: 'Group 3' }, { status: 201, statusText: 'Created' });
    await settle();

    httpTesting.expectOne({ method: 'GET', url: '/api/student-groups' }).flush([{ id: 3, name: 'Group 3' }]);
    await fixture.whenStable();

    expect(groupNames()).toEqual(['Group 3']);
    expect(page.querySelector<HTMLInputElement>('#group-name')!.value).toBe('');
  });

  it('shows the backend error for a duplicate name', async () => {
    httpTesting.expectOne('/api/student-groups').flush([{ id: 3, name: 'Group 3' }]);
    await fixture.whenStable();

    type('group 3');
    await submit();
    httpTesting.expectOne({ method: 'POST', url: '/api/student-groups' }).flush(
      { title: 'Conflict', status: 409, detail: 'A student group named "group 3" already exists.' },
      { status: 409, statusText: 'Conflict' }
    );
    await settle();

    expect(page.querySelector('[role="alert"]')?.textContent).toContain(
      'A student group named "group 3" already exists.'
    );
  });

  it('does not send a student group without a name', async () => {
    httpTesting.expectOne('/api/student-groups').flush([]);
    await fixture.whenStable();

    await submit();

    httpTesting.expectNone({ method: 'POST', url: '/api/student-groups' });
    expect(page.querySelector('#group-name-error')?.textContent).toContain('Please enter a name.');
  });
});
