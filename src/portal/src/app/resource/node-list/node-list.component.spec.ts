import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { Directive, Input, Output, EventEmitter, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { of } from 'rxjs/observable/of';
import { Router, ActivatedRoute } from '@angular/router';
import { ApiService } from '../../services/api.service';
import { FormsModule } from '@angular/forms'
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MatDialog } from '@angular/material';
import { MaterialsModule } from '../../materials.module';
import { NodeListComponent } from './node-list.component';
import { TableService } from '../../services/table/table.service';
import { ScrollingModule, CdkVirtualScrollViewport } from '@angular/cdk/scrolling';

@Directive({
  selector: '[routerLink]',
  host: { '(click)': 'onClick()' }
})
class RouterLinkDirectiveStub {
  @Input('routerLink') linkParams: any;
  navigatedTo: any = null;

  onClick() {
    this.navigatedTo = this.linkParams;
  }
}

@Directive({
  selector: '[appWindowScroll]',
})
class WindowScrollDirectiveStub {
  @Input() dataLength: number;
  @Input() pageSize: number;
  @Output() scrollEvent = new EventEmitter();
}

const routerStub = {
  navigate: () => { },
}

const activatedRouteStub = {
  queryParamMap: of({ get: () => undefined })
}

const matDialogStub = {
  open: () => { }
}

class ApiServiceStub {
  static nodes = [{
    id: 'a node',
    name: 'a node',
    state: 'Online',
    health: 'OK',
    nodeRegistrationInfo: {
      memoryMegabytes: 6947,
      distroInfo: 'Linux'

    },
    runningJobCount: 1,
    eventCount: 1
  }]

  node = {
    getAll: () => of(ApiServiceStub.nodes),
    getNodesByPage: () => of(ApiServiceStub.nodes)
  }

  command = {
    create: () => of({ id: 1 })
  }
}

const TableServiceStub = {
  updateDatasource: (newData, dataSource, propertyName) => dataSource.data = newData,
  loadSetting: (key, initVal) => initVal,
  saveSetting: (key, val) => undefined,
  isContentScrolled: () => false
}

fdescribe('NodeListComponent', () => {
  let component: NodeListComponent;
  let fixture: ComponentFixture<NodeListComponent>;
  let viewport: CdkVirtualScrollViewport;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [
        RouterLinkDirectiveStub,
        NodeListComponent,
        WindowScrollDirectiveStub
      ],
      imports: [
        NoopAnimationsModule,
        FormsModule,
        MaterialsModule,
        ScrollingModule
      ],
      providers: [
        { provide: ApiService, useClass: ApiServiceStub },
        { provide: TableService, useValue: TableServiceStub },
        { provide: Router, useValue: routerStub },
        { provide: ActivatedRoute, useValue: activatedRouteStub }
      ],
      schemas: [CUSTOM_ELEMENTS_SCHEMA]
    })
      //We want to stub out MatDialog only for this component.
      .overrideComponent(NodeListComponent, {
        add: {
          providers: [
            { provide: MatDialog, useValue: matDialogStub },
          ],
        }
      })
      .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(NodeListComponent);
    component = fixture.componentInstance;
    viewport = component.cdkVirtualScrollViewport;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
    expect(viewport.getDataLength()).toEqual(1);
  });

  it('should run command', () => {
    let cmd = 'x command';
    let timeout = 1800;
    let params = { command: cmd, timeout: timeout };
    let dialogRef = jasmine.createSpyObj('dialogRef', ['afterClosed']);
    dialogRef.afterClosed.and.returnValue(of(params));
    let dialog = fixture.debugElement.injector.get(MatDialog);
    spyOn(dialog, 'open').and.returnValue(dialogRef);
    let api = fixture.debugElement.injector.get(ApiService);
    spyOn(api.command, 'create').and.callThrough();
    let router = fixture.debugElement.injector.get(Router);
    spyOn(router, 'navigate').and.callThrough();
    component.runCommand();
    expect(dialog.open).toHaveBeenCalled();
    expect(api.command.create).toHaveBeenCalledWith(cmd, [], timeout);
    expect(router.navigate).toHaveBeenCalledWith(['/command/results/1']);
  });

  it('should not run command', () => {
    let cmd = false;
    let dialogRef = jasmine.createSpyObj('dialogRef', ['afterClosed']);
    dialogRef.afterClosed.and.returnValue(of(cmd));
    let dialog = fixture.debugElement.injector.get(MatDialog);
    spyOn(dialog, 'open').and.returnValue(dialogRef);
    let api = fixture.debugElement.injector.get(ApiService);
    spyOn(api.command, 'create').and.callThrough();
    let router = fixture.debugElement.injector.get(Router);
    spyOn(router, 'navigate').and.callThrough();
    component.runCommand();
    expect(dialog.open).toHaveBeenCalled();
    expect(api.command.create).not.toHaveBeenCalled();
    expect(router.navigate).not.toHaveBeenCalled();
  });
});
