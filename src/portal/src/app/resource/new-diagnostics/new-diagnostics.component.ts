import { Component, OnInit, ViewChild, Inject, HostListener, ChangeDetectorRef } from '@angular/core';
import { MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';
import { NodeFilterBuilderComponent } from '../../widgets/node-filter-builder/node-filter-builder.component';
import { ApiService } from '../../services/api.service';
import { FormGroup, FormBuilder, Validators, FormControl } from '@angular/forms';

@Component({
  selector: 'diagnostics-tests',
  templateUrl: './new-diagnostics.component.html',
  styleUrls: ['./new-diagnostics.component.scss']
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
  private paraForm: FormGroup;
  private ctlConfig = {};

  constructor(
    private api: ApiService,
    public dialogRef: MatDialogRef<NewDiagnosticsComponent>,
    private cdRef: ChangeDetectorRef,
    private fb: FormBuilder,
    @Inject(MAT_DIALOG_DATA) public data: any
  ) {
  }


  ngOnInit() {
    this.api.diag.getDiagTests().subscribe(tests => {
      this.tests = tests.treeData;
      tests['rawData'].forEach(t => {
        if (t.parameters) {
          this.ctlConfig[t.name] = {};
          t.parameters.forEach(p => {
            if (p.type == 'number') {
              this.ctlConfig[t.name][p.name] = [p.defaultValue, [Validators.required, Validators.min(p.min), Validators.max(p.max)]];
            }
            else {
              this.ctlConfig[t.name][p.name] = [p.defaultValue, [Validators.required]];
            }
          });
        }
      });
    });
  }

  ngAfterViewChecked() {
    this.cdRef.detectChanges();
  }

  onTreeLoad(tree) {
    tree.treeModel.expandAll();
  }

  private check(node, checked) {
    this.selectedDescription = '';
    this.testInfoLink = '';
    if (checked) {
      this.selectedDescription = node.data.description;
      if (this.selectedDescription) {
        let index = this.selectedDescription.indexOf('http');
        if (index != -1) {
          this.testInfoLink = this.selectedDescription.substr(index);
          this.selectedDescription = this.selectedDescription.substr(0, index);
        }
      }
    }
    this.updateCheckedNode(node, checked);
  }

  private updateCheckedNode(node: any, checked: any) {
    let allNodes = node.treeModel.nodes;
    for (let i = 0; i < allNodes.length; i++) {
      for (let j = 0; j < allNodes[i].children.length; j++) {
        allNodes[i].children[j].checked = false;
      }
    }
    node.data.checked = checked;
    if (checked) {
      this.selectedTest = node.data;
      this.paraForm = this.fb.group(this.ctlConfig[this.selectedTest.name]);
      let paras = this.selectedTest.parameters;
      for (let i = 0; i < paras.length; i++) {
        this.paraForm.controls[paras[i].name].valueChanges.subscribe(data => {
          if (paras[i].whenChanged != undefined) {
            let selected = paras[i].whenChanged[data];
            for (let key in selected) {
              this.paraForm.controls[key].setValue(selected[key]);
            }
          }
        });
      }
      this.diagTestName = `${this.selectedTest.name} created by`;
    }
    else {
      this.selectedTest = undefined;
      this.diagTestName = `created by`;
    }
  }


  selectionChange(e) {
    if (e.selectedIndex == 2) {
      if (this.selectedTest != undefined) {
        this.diagTestName = `${this.selectedTest.name} created by`;
      }
      else {
        this.diagTestName = `created by`;
      }
    }
  }

  getTest() {
    if (this.selectedTest == undefined) {
      this.errorMessage = 'Please select one test to run in step 1 !';
    }
    else if (this.diagTestName == undefined || this.diagTestName == "") {
      this.errorMessage = 'Please enter the diagnostic name in step 3 ! ';
    }
    else {
      if (this.selectedTest.parameters != undefined) {
        let paras = this.selectedTest.parameters;
        let args = [];
        for (let i = 0; i < paras.length; i++) {
          if (this.paraForm.controls[paras[i].name].invalid) {
            this.errorMessage = 'Please enter valid parameters in step 2 !';
            return;
          }
          args.push({ name: paras[i].name, value: this.paraForm.controls[paras[i].name].value });
        }
        this.selectedTest.arguments = args;
      }
      let diagInfo = { selectedTest: this.selectedTest, diagTestName: this.diagTestName };
      this.dialogRef.close(diagInfo);
    }
  }

  clearErrorMsg() {
    this.errorMessage = "";
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
