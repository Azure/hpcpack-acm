import { Component, OnInit, ViewChild, Inject } from '@angular/core';
import { MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';
// import { TreeComponent } from 'angular-tree-component';
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
  private selectedTests = [];
  private selectedTestsWithParameters = []
  private nodeFilter: string = '';
  private selectedDescription: string;
  private testInfoLink: string;
  private errorMessage: string;
  private diagTestName: string;

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
    this.updateChildNodeCheckbox(node, checked);
    this.updateParentNodeCheckbox(node.realParent);
  }

  private updateChildNodeCheckbox(node, checked) {
    node.data.checked = checked;
    if (node.children) {
      node.children.forEach((child) => this.updateChildNodeCheckbox(child, checked));
    }
  }

  private updateParentNodeCheckbox(node) {
    if (!node) {
      return;
    }

    let allChildrenChecked = true;
    let noChildChecked = true;

    for (const child of node.children) {
      if (!child.data.checked || child.data.indeterminate) {
        allChildrenChecked = false;
      }
      if (child.data.checked) {
        noChildChecked = false;
      }
    }

    if (allChildrenChecked) {
      node.data.checked = true;
      node.data.indeterminate = false;
    }
    else if (noChildChecked) {
      node.data.checked = false;
      node.data.indeterminate = false;
    }
    else {
      node.data.checked = true;
      node.data.indeterminate = true;
    }
    this.updateParentNodeCheckbox(node.parent);
  }

  private getSelectedTests(node: any): string[] {
    if (!node.checked) {
      return [];
    }
    let array = [];
    if (node.children) {
      for (let i = 0; i < node.children.length; i++) {
        array = array.concat(this.getSelectedTests(node.children[i]));
      }
    }
    return array.length > 0 ? array : [node];
  }

  private selectTests(): void {
    this.selectedTests = this.getSelectedTests(this.tests[0]);
    this.selectedTestsWithParameters = this.selectedTests.filter(test => test.parameters);
  }

  getTests() {
    //To integrate selecedTests and selectedTestsWithParameters later
    let selectedTests = this.getSelectedTests(this.tests[0]);

    if (selectedTests.length == 0) {
      this.errorMessage = 'Please select at least one test to run in Step 1 !';
    }
    else if(this.diagTestName == undefined){
      this.errorMessage = 'Please enter the diagnostic name in step 3 ! ';
    }
    else {
      let diagInfo = {selectedTests: selectedTests, diagTestName: this.diagTestName};
      this.dialogRef.close(diagInfo);
    }

  }
}
