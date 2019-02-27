import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { CommandOutputComponent } from './command-output.component';
import { MaterialsModule } from '../../materials.module';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';

fdescribe('CommandOutputComponent', () => {
  let component: CommandOutputComponent;
  let fixture: ComponentFixture<CommandOutputComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [CommandOutputComponent],
      imports: [MaterialsModule, NoopAnimationsModule]
    })
      .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(CommandOutputComponent);
    component = fixture.componentInstance;
    component.content = 'test content';
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
    let text = fixture.nativeElement.querySelector('pre').textContent;
    expect(text).toEqual('test content');
  });
});
