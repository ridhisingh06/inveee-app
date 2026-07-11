import { Pipe, PipeTransform } from '@angular/core';
import { FormGroup } from '@angular/forms';
import { IssuerPendingItem, AdminPendingItem } from '../models/request.model';

/** Sum of requestedQuantity across issuer pending items */
@Pipe({ name: 'totalRequested', standalone: true, pure: false })
export class TotalRequestedPipe implements PipeTransform {
  transform(items: (IssuerPendingItem | AdminPendingItem)[]): number {
    return (items ?? []).reduce((s, i) => s + (i.requestedQuantity ?? 0), 0);
  }
}

/** Sum of issueQty across a request FormGroup's rows FormArray */
@Pipe({ name: 'totalIssue', standalone: true, pure: false })
export class TotalIssuePipe implements PipeTransform {
  transform(fg: FormGroup | null): number {
    if (!fg) return 0;
    const rows = (fg.get('rows') as any)?.controls ?? [];
    return rows.reduce((s: number, r: any) => s + (parseInt(r.get('issueQty')?.value, 10) || 0), 0);
  }
}

/** Sum of rejectQty across a request FormGroup's rows FormArray */
@Pipe({ name: 'totalReject', standalone: true, pure: false })
export class TotalRejectPipe implements PipeTransform {
  transform(fg: FormGroup | null): number {
    if (!fg) return 0;
    const rows = (fg.get('rows') as any)?.controls ?? [];
    return rows.reduce((s: number, r: any) => s + (parseInt(r.get('rejectQty')?.value, 10) || 0), 0);
  }
}

/** Sum of approveQty across a request FormGroup's rows FormArray (admin) */
@Pipe({ name: 'totalApprove', standalone: true, pure: false })
export class TotalApprovePipe implements PipeTransform {
  transform(fg: FormGroup | null): number {
    if (!fg) return 0;
    const rows = (fg.get('rows') as any)?.controls ?? [];
    return rows.reduce((s: number, r: any) => s + (parseInt(r.get('approveQty')?.value, 10) || 0), 0);
  }
}

/** Sum of issuerIssuedQuantity across admin pending items */
@Pipe({ name: 'totalIssued', standalone: true, pure: false })
export class TotalIssuedPipe implements PipeTransform {
  transform(items: AdminPendingItem[]): number {
    return (items ?? []).reduce((s, i) => s + ((i as any).issuerIssuedQuantity ?? 0), 0);
  }
}
