type
Person2 = class
  private
    fName1: string;
    fAge: integer;
    a1: Person2;
    procedure Print2;
end;

type
Person1 = class(Person2)
  private
    fAge: integer;
    a1: Person2;
    fPerson: Person2;
  public
    constructor Create(Name: string; Age: integer);
    begin
      fName1 := Name;
      fAge := Age;
    end;
    procedure Print3;
    property Name: string read fName1;
    property Age: integer read fAge;
  end;

procedure Person1.Print3;
begin
  Writeln('aaaaa');
end;

procedure Person2.Print2;
begin
  Writeln('aaaaa');
end;

begin
end.