type
  BHModule = abstract class
    
    property p: byte read; abstract;
    
    public static b1: byte;
    
  end;

begin
  var a := BHModule.b1;
  var b := BHModule.p;
end.