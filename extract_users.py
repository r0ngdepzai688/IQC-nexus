import pandas as pd
import json
import math

df = pd.read_excel('d:/Code_viber/Portal/User_DB.xlsx', header=1) # The first row might be title, header=1 to use the second row as columns? 
# Wait, looking at the previous output, line 1 was:
# {"Unnamed: 0":"STT","Unnamed: 1":"Mã nhân viên","Unnamed: 2":"Tên","Unnamed: 3":"Bộ phận","Unnamed: 4":"Tổ chức","Unnamed: 5":"CL Name","Unnamed: 6":"Knox ID","Unnamed: 7":"Chức vụ","Unnamed: 8":"Scope"}
# This means header=1 is correct (STT, Mã nhân viên, Tên...)
df = pd.read_excel('d:/Code_viber/Portal/User_DB.xlsx', header=1)
df.fillna('', inplace=True)

# Drop duplicates by Employee ID
df.drop_duplicates(subset=['Mã nhân viên'], inplace=True)

users = []
for index, row in df.iterrows():
    emp_id = str(row['Mã nhân viên']).strip()
    if not emp_id or emp_id == 'nan' or emp_id == '':
        continue
        
    users.append({
        "EmployeeId": emp_id,
        "Name": str(row['Tên']).strip(),
        "Department": str(row['Bộ phận']).strip(),
        "Organization": str(row['Tổ chức']).strip(),
        "CLName": str(row['CL Name']).strip(),
        "KnoxId": str(row['Knox ID']).strip().replace(';', ''),
        "Position": str(row['Chức vụ']).strip(),
        "Scope": str(row['Scope']).strip()
    })

with open('d:/Code_viber/Portal/backend/src/IqcQms.Infrastructure/Data/Seeders/users_seed.json', 'w', encoding='utf-8') as f:
    json.dump(users, f, ensure_ascii=False, indent=2)
print(f"Extracted {len(users)} unique users.")
