import { Component } from '@angular/core';
import { Router, RouterOutlet, RouterModule } from '@angular/router';
import { NavbarComponent } from './navbar/navbar';
import { ClickLoggerDirective } from './click-logger.directive';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, NavbarComponent, RouterModule, ClickLoggerDirective],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent {
  constructor(private router: Router) {
    this.router.events.subscribe(event => {
      const logBase = '[ROUTER]';
      // @ts-ignore - Event type guards handled below
      if (event && (event as any).constructor) {
        const type = (event as any).constructor.name;
        switch (type) {
          case 'NavigationStart':
            console.log(`${logBase} NavigationStart`, '\nurl:', (event as any).url);
            break;
          case 'RouteConfigLoadStart':
            console.log(`${logBase} RouteConfigLoadStart`, '\nroute:', (event as any).route?.path);
            break;
          case 'RouteConfigLoadEnd':
            console.log(`${logBase} RouteConfigLoadEnd`, '\nroute:', (event as any).route?.path);
            break;
          case 'RoutesRecognized':
            console.log(`${logBase} RoutesRecognized`, '\nurl:', (event as any).url, '\nurlAfterRedirects:', (event as any).urlAfterRedirects);
            break;
          case 'GuardsCheckStart':
            console.log(`${logBase} GuardsCheckStart`, '\nurl:', (event as any).url);
            break;
          case 'ChildActivationStart':
            console.log(`${logBase} ChildActivationStart`, '\nroute:', (event as any).snapshot?.routeConfig?.path);
            break;
          case 'ActivationStart':
            console.log(`${logBase} ActivationStart`, '\nroute:', (event as any).snapshot?.routeConfig?.path);
            break;
          case 'GuardsCheckEnd':
            console.log(`${logBase} GuardsCheckEnd`, '\nurl:', (event as any).url);
            break;
          case 'ResolveStart':
            console.log(`${logBase} ResolveStart`, '\nurl:', (event as any).url);
            break;
          case 'ResolveEnd':
            console.log(`${logBase} ResolveEnd`, '\nurl:', (event as any).url);
            break;
          case 'NavigationEnd':
            console.log(`${logBase} NavigationEnd`, '\nurlAfterRedirects:', (event as any).urlAfterRedirects);
            break;
          case 'NavigationCancel':
            console.log(`${logBase} NavigationCancel`, '\nurl:', (event as any).url, '\nreason:', (event as any).reason);
            break;
          case 'NavigationError':
            console.error(`${logBase} NavigationError`, '\nurl:', (event as any).url, '\nerror:', (event as any).error);
            break;
          case 'Scroll':
            console.log(`${logBase} Scroll`, '\nposition:', (event as any).position);
            break;
          default:
            console.log(`${logBase} ${type}`, event);
        }
      }
    });
  }
  title = 'Online Stationary Management System';
}
