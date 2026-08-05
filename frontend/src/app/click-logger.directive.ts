import { Directive, HostListener, ElementRef } from '@angular/core';
import { RuntimeLoggerService } from './runtime-logger.service';

@Directive({
  selector: 'button',
  standalone: true
})
export class ClickLoggerDirective {
  constructor(private logger: RuntimeLoggerService, private el: ElementRef) {}

  @HostListener('click', ['$event'])
  handleClick(event: MouseEvent) {
    const native = this.el.nativeElement as HTMLButtonElement;
    const disabled = native.disabled;
    const type = native.getAttribute('type') || 'button';
    const classes = native.className;
    const innerText = native.innerText.trim();
    this.logger.log({
      timestamp: new Date(),
      type: 'CLICK',
      details: {
        disabled,
        buttonType: type,
        classes,
        innerText,
        eventType: event.type,
        defaultPrevented: event.defaultPrevented
      }
    });
  }
}
