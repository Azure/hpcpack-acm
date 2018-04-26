import { Component, OnInit, ViewChild, Inject, HostListener } from '@angular/core';
import { MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';
import { NodeFilterBuilderComponent } from '../../widgets/node-filter-builder/node-filter-builder.component';
import { ApiService } from '../../services/api.service';

@Component({
  selector: 'diagnostics-tests',
  templateUrl: './new-diagnostics.component.html',
  styleUrls: ['./new-diagnostics.component.css']
})
export class NewDiagnosticsComponent implements OnInit {
  @ViewChild('tree') tree;

  private tests = [];
  private nodeFilter: string = '';
  private selectedDescription: string;
  private testInfoLink: string;
  private errorMessage: string;
  private diagTestName: string;
  private selectedTest: any;
  private selectedTestWithParameters: any;

  constructor(
    private api: ApiService,
    public dialogRef: MatDialogRef<NewDiagnosticsComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any
  ) { }

  ngOnInit() {
    this.api.diag.getDiagTests().subscribe(tests => {
      this.tests = tests;
    });
  }

  onTreeLoad(tree) {
    tree.treeModel.expandAll();
  }

  private check(node, checked) {
    this.selectedDescription = node.data.description;
    if (this.selectedDescription) {
      let index = this.selectedDescription.indexOf('http');
      if (index != -1) {
        this.testInfoLink = this.selectedDescription.substr(index);
        this.selectedDescription = this.selectedDescription.substr(0, index);
      }
    }
    else {
      this.selectedDescription = '';
      this.testInfoLink = '';
    }
    if (checked) {
      this.updateCheckedNode(node);
    }
  }

  private updateCheckedNode(node: any) {
    let allNodes = node.treeModel.nodes;
    for (let i = 0; i < allNodes.length; i++) {
      for (let j = 0; j < allNodes[i].children.length; j++) {
        allNodes[i].children[j].checked = false;
      }
    }

    node.data.checked = true;
    this.selectedTest = node.data;
  }

  // function to handle default value when one linked property's value changes
  private whenChange(e, p, t) {
    //e is element of parameters, p is parameters. t is the selected test
    if (p.whenChanged != undefined) {
      let selected = p.whenChanged[e];
      for (let key in selected) {
        let index = t.parameters.findIndex((item) => {
          return item.name == key;
        });
        if (index != -1) {
          t.parameters[index].defaultValue = selected[key];
        }
      }
    }
  }

  getTest() {
    if (this.selectedTest == undefined) {
      this.errorMessage = 'Please select one test to run in Step 1 !';
    }
    else if (this.diagTestName == undefined) {
      this.errorMessage = 'Please enter the diagnostic name in step 3 ! ';
    }
    else {
      let args = this.selectedTest.parameters.map((item) => {
        return { name: item.name, value: item.defaultValue };
      });
      this.selectedTest.arguments = args;

      let diagInfo = { selectedTest: this.selectedTest, diagTestName: this.diagTestName };
      this.dialogRef.close(diagInfo);
    }
  }

  @HostListener('window:keyup', ['$event'])
  keyEvent(event: KeyboardEvent) {
    if (event.key == 'Enter') {
      this.getTest();
    }
  }

  close() {
    this.dialogRef.close();
  }
}
