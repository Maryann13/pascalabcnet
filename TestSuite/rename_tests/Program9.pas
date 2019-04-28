uses A;

var
  Foo5: integer := 5;

type
Person2 = class
  private
    Foo7: string;
    fName1: string;
    fAge: integer;
    a1: Person2;
    procedure Print2;
end;

type
Person4 = class
  private
    fName1: string;
    fAge: integer;
    a1: Person2;
    fPerson: Person2;
  public
    constructor Create(Name: string; Age: integer);
    begin
      fName1 := Name;
      fAge := Age;
    end;
    procedure Print4;
    property Name1: string read fName1;
    property Age: integer read fAge;
  end;

procedure Person4.Print4;
begin
  Writeln($'Имя: {Name1} Возраст: {Age}');
end;

procedure Person2.Print2;
begin
  Writeln($'Имя: {fName1} Возраст: {fAge}');
end;

function Foo3(): Person4;
begin
  Result := new Person4('a', 5);
end;

function Foo3(a: integer): Person2;
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
  var a1 := Foo3();
  a1.a1.a1.Print2();
  var Foo4: Person2 := Foo3(5);
  //var Foo1 := a4;
  for var i := 0 to 5 do
  begin
    a1.a1.a1.Print2();
    Foo4.a1.Print2();
    var a2 := A.a3;
    var Foo6: Person2 := Foo3(5);
  end;
end.