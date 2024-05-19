import json


test = json.loads("[{\u0022id\u0022:\u0022xyz1\u0022,\u0022name\u0022:\u0022Hue color lamp 1\u0022,\u0022on\u0022:true,\u0022brightness\u0022:254,\u0022hexColor\u0022:\u0022FEDE72\u0022}]")

print(test[0])