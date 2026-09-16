import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CompanyForm } from './features/companies/company-form/company-form';

@Component({
  imports: [RouterOutlet, CompanyForm],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {}
