import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ApiService {

  private base = 'http://localhost:8000'; // tu .NET

  constructor(private http: HttpClient) {}

  get<T>(url: string) {
    return this.http.get<T>(this.base + url, { withCredentials: true });
  }

  post<T>(url: string, body: any) {
    return this.http.post<T>(this.base + url, body, { withCredentials: true });
  }
}