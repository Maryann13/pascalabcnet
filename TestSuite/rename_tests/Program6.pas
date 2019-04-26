

uses A;

type
Person2 = class
  private
    fName1: string;
    fAge: integer;
    a1: Person2;
    procedure Print2;
end;

type
Person1 = class
  private
    fName1: string;
    fAge: integer;
    a1: Person2;
    fPerson: Person2;
  public
    constructor Create(Name: string; Age: integer);
    begin
      Self.fName1 := Name;
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

function Foo1(): Person1;
begin
  Result := new Person1('a', 5);
end;

var
  Foo1: integer := 5;

function Foo1(a: integer): Person2;
begin
  Result := new Person2();
end;

function FindFirstInArray<T>(a: array of T; val: T): integer;
begin
  Result := -1;
  for var i:=0 to a.Length-1 do
    if a[i]=val then
    begin
      Result := i;
      exit;
    end;
end;

begin
  var f: integer -> integer := x1 -> x1 * x1;
  var a13: integer; var a12: integer;
  var a1: Person1 := Foo1();
  var a2: Person1 := Foo1();
  var a1: Person1 := Foo1();
  var Foo1: Person2 := Foo1(5);
  var d := a1;
  for var i := 0 to 5 do
  begin
    var a3: Person2 := Foo1(5);
    a2.a1.a1.Print2();
    a1.a1.a1.Print2();
    a1.a1.Print2();
    Person1.a1.Print2();
    Foo1.a1.Print2();
    var Foo1: Person2 := Foo1(5);
    var Person1: Person2 := Foo1(5);
    var b := A.a3;
  end;
  var c := d.Name;
end.