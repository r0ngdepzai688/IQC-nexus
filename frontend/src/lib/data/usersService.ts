import usersData from '@/data/users.json';

export interface UserData {
  empId: string;
  name: string;
  department: string;
  organization: string;
  clName: string;
  email: string;
  position: string;
  scope: string;
}

// Convert the Excel-exported JSON array into a typed array
export const getAllUsers = (): UserData[] => {
  if (!Array.isArray(usersData)) return [];
  
  return usersData
    .filter(row => row["Unnamed: 1"] && row["Unnamed: 1"] !== "Mã nhân viên")
    .map(row => ({
      empId: row["Unnamed: 1"]?.toString() || "",
      name: row["Unnamed: 2"] || "Unknown",
      department: row["Unnamed: 3"] || "",
      organization: row["Unnamed: 4"] || "",
      clName: row["Unnamed: 5"] || "",
      email: row["Unnamed: 6"] || "",
      position: row["Unnamed: 7"] || "",
      scope: row["Unnamed: 8"] || "",
    }));
};

// Create a fast lookup map (singleton behavior in module scope)
let userMap: Map<string, UserData> | null = null;

export const getUserById = (empId: string): UserData | null => {
  // If the passed empId starts with '@', strip it for lookup, though standard is no '@'
  const cleanId = empId.startsWith('@') ? empId.substring(1) : empId;
  
  if (!userMap) {
    userMap = new Map();
    getAllUsers().forEach(u => userMap!.set(u.empId, u));
  }
  
  return userMap.get(cleanId) || null;
};
