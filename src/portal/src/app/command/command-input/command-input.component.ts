import { Component, OnInit, Inject } from '@angular/core';
import { MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';

@Component({
  templateUrl: './command-input.component.html',
  styleUrls: ['./command-input.component.css']
})
export class CommandInputComponent implements OnInit {
  private command: string = '';

  constructor(@Inject(MAT_DIALOG_DATA) public data: any) {
    this.command = data.command;
  }

  ngOnInit() {}

}
