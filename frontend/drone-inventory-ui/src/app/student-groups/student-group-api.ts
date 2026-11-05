import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { Observable } from 'rxjs';
import { CreateStudentGroupRequest, StudentGroup } from '../api/api-models';

@Service()
export class StudentGroupApi {
  private readonly http = inject(HttpClient);

  list(): Observable<StudentGroup[]> {
    return this.http.get<StudentGroup[]>('/api/student-groups');
  }

  create(request: CreateStudentGroupRequest): Observable<StudentGroup> {
    return this.http.post<StudentGroup>('/api/student-groups', request);
  }
}
