import { Component, input } from '@angular/core';
import { DspAssignment } from '../../models/track.models';

@Component({
  standalone: false,
  selector: 'app-dsp-distribution-table',
  templateUrl: './dsp-distribution-table.component.html',
})
export class DspDistributionTableComponent {
  readonly assignments = input.required<readonly DspAssignment[]>();
}
