import { Component } from '@angular/core';
import { NavbarComponent } from '../navbar/navbar';

@Component({
  standalone: true,
  selector: 'app-my-requests',
  imports: [NavbarComponent],
  templateUrl: './my-requests.html',
  styleUrls: ['./my-requests.css']
})
export class MyRequestsComponent {
  // Component logic goes here
}
