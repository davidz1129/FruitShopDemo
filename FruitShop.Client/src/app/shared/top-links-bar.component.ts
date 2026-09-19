import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-top-links-bar',
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './top-links-bar.component.html',
  styleUrl: './top-links-bar.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TopLinksBarComponent {}