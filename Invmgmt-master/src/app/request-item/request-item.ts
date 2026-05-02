import { Component } from '@angular/core';
import { NavbarComponent } from '../navbar/navbar';

@Component({
  standalone: true,
  selector: 'app-request-item',
  imports: [NavbarComponent],
  templateUrl: './request-item.html',
  styleUrls: ['./request-item.css']

})
export class RequestItemComponent {
  // Component logic goes here
}
