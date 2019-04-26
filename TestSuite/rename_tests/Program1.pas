type
Person2<T> = class
  private
    fName1: string;
    fAge: integer;
    a1: Person2<T>;
    a2: T;
    procedure Print2;
end;

type
Person1<T> = class
  private
    fName1: string;
    fAge: integer;
    a1: Person2<T>;
    fPerson: Person2<T>;
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

procedure Person1<T>.Print3;
begin
  Writeln('aaaaa');
end;

procedure Person2<T>.Print2;
begin
  Writeln('aaaaa');
end;

function Foo1<T>(): Person1<T>;
begin
  Result := new Person1<T>('a', 5);
end;

function Foo1<T>(a: integer): Person2<T>;
begin
  Result := new Person2<T>();
end;

function FindFirstInArray<T,Q>(a: array of Q; val: T): integer;
begin
  Result := -1;
  for var i:=0 to a.Length-1 do
    if a[i]=val then
    begin
      var a2: T;
      Result := i;
      exit;
    end;
end;

begin
  var f: integer -> integer := x1 -> x1 * x1;
  var a2: Person1<integer> := Foo1&<integer>();
  var a1: Person2<integer> := Foo1&<integer>(5);
  var d := a2;
  for var i := 0 to 5 do
  begin
    a2.a1.a1.Print2();
    a1.a1.a1.Print2();
    a1.a1.Print2();
  end;
  var c := d.Name;
end.