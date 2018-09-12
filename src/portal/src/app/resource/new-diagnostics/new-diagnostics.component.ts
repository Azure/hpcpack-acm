import { Component, OnInit, ViewChild, Inject, HostListener, ChangeDetectorRef, OnDestroy } from '@angular/core';
import { MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';
import { NodeFilterBuilderComponent } from '../../widgets/node-filter-builder/node-filter-builder.component';
import { ApiService } from '../../services/api.service';
import { FormGroup, FormBuilder, Validators, FormControl } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'diagnostics-tests',
  templateUrl: './new-diagnostics.component.html',
  styleUrls: ['./new-diagnostics.component.scss']
})
export class NewDiagnosticsComponent implements OnInit, OnDestroy {
  @ViewChild('tree') tree;

  public tests = [];
  private nodeFilter: string = '';
  public selectedDescription: string;
  private testInfoLink: string;
  public errorMessage: string;
  public diagTestName: string;
  public selectedTest: any;
  private selectedTestWithParameters: any;
  private paraForm: FormGroup;
  private ctlConfig = {};

  constructor(
    private api: ApiService,
    public dialogRef: MatDialogRef<NewDiagnosticsComponent>,
    private cdRef: ChangeDetectorRef,
    private fb: FormBuilder,
    private authService: AuthService,
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
            let validators = [];
            if (p.required) {
              validators.push(Validators.required);
            }
            if (p.type == 'number') {
              validators.push(Validators.min(p.min));
              validators.push(Validators.max(p.max));
            }
            this.ctlConfig[t.name][p.name] = [p.defaultValue, validators];
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

  private _sub: Subscription[];
  private updateCheckedNode(node: any, checked: any) {
    let allNodes = node.treeModel.nodes;
    for (let i = 0; i < allNodes.length; i++) {
      for (let j = 0; j < allNodes[i].children.length; j++) {
        allNodes[i].children[j].checked = false;
      }
    }
    node.data.checked = checked;
    this._sub = new Array<Subscription>();
    if (checked) {
      this.selectedTest = node.data;
      let paras = this.selectedTest.parameters;
      if (paras) {
        this.paraForm = this.fb.group(this.ctlConfig[this.selectedTest.name]);
        for (let i = 0; i < paras.length; i++) {
          let sub = this.paraForm.controls[paras[i].name].valueChanges.subscribe(data => {
            if (paras[i].whenChanged != undefined) {
              let selected = paras[i].whenChanged[data];
              for (let key in selected) {
                this.paraForm.controls[key].setValue(selected[key]);
              }
            }
          });
          this._sub.push(sub);
        }
      }
      this.diagTestName = `${this.selectedTest.name} created by ${this.authService.username}`;
    }
    else {
      this.selectedTest = undefined;
      this.diagTestName = `created by ${this.authService.username}`;
    }
  }

  ngOnDestroy() {
    if (this._sub) {
      this._sub.forEach(sub => {
        sub.unsubscribe();
      })
    }
  }

  selectionChange(e) {
    if (e.selectedIndex == 2) {
      if (this.selectedTest != undefined) {
        this.diagTestName = `${this.selectedTest.name} created by ${this.authService.username}`;
      }
      else {
        this.diagTestName = `created by ${this.authService.username}`;
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

  close() {
    this.dialogRef.close();
  }
}
