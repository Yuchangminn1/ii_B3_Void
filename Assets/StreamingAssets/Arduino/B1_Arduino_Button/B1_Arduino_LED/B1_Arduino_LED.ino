int ReturnCount = 0;
void setup() {
  Serial.begin(9600);
  Serial.setTimeout(50); 

}

void loop() {
  if (Serial.available() > 0) {
   
    String data = Serial.readString();
    ReturnCount = 0;
    data.trim(); // 앞뒤 공백 및 줄바꿈 제거

    // 2. "IN"으로 시작하고 "OUT"이 포함되어 있는지 검사
    if (data.startsWith("IN") ) {
      
      if(data.indexOf("OUT") == -1) 
      {
        Serial.println("Please Add Array Tail To OUT   EX : IN,1,2,3,4,5OUT");
        return;
      }

      //Serial.println(data);
      int startIdx = 2; // "IN"이 2글자이므로 index 2부터 시작
      int endIdx = data.indexOf("OUT");
      String payload = data.substring(startIdx, endIdx); // 예: "1,2,3"
      parseAndCheck(payload);

    } 
    else 
    {
      // 형식이 맞지 않는 경우
      Serial.println("Please Add Array Head To IN   EX : IN,1,2,3,4,5OUT");
    }
      delay(50);

  }
  else 
  {
    ReturnCount++;
    if(ReturnCount>30)
    {
      parseAndCheck("");
      ReturnCount = 0;
      delay(50);

    }
    delay(50);
  }
}

void parseAndCheck(String numbers) {
  String OutMessage = "IN";

  while (numbers.length() > 0) 
  {
    int commaIndex = numbers.indexOf(',');
    String valStr;

    if (commaIndex != -1) {
      valStr = numbers.substring(0, commaIndex);
      numbers = numbers.substring(commaIndex + 1);
    } else {
      // 마지막 숫자 처리
      valStr = numbers;
      numbers = "";
    }

    if (valStr.length() > 0) {
      OutMessage+=String(valStr.toInt() );
    }
    if(numbers.length() > 0)
    {
      OutMessage+=",";
    }
  }
  OutMessage+="OUT";

  Serial.println(OutMessage);
}

