import { Component, OnInit, Input } from '@angular/core';

@Component({
  selector: 'app-loading-progress-bar',
  templateUrl: './loading-progress-bar.component.html',
  styleUrls: ['./loading-progress-bar.component.scss']
})
export class LoadingProgressBarComponent implements OnInit {
  @Input()
  loadFinished: boolean;

  constructor() { }

  ngOnInit() {
  }

}
