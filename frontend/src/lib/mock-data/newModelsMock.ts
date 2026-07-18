export interface MasterPlanRecord {
    id: number;
    projectName: string;
    basic: string;
    areaRegion: string;
    grade: string;
    sku: string;
    category: string; // Cat (LPR/LSR)
    qtyLpr: number;
    qtyLsr: number;
    prePvrTargetDate: string | null;
    praTargetDate: string | null;
    sraTargetDate: string | null;
    picIqc: string;
    actionStatus: 'Urgent' | 'Ready' | 'Future' | 'Active';
    isActivated: boolean;
}

export type WorkspaceStatus =
    | 'Preparation'
    | 'InProgress'
    | 'Completed';

export interface ProjectWorkspace {
    id: number;
    sourceRecordId: number;
    projectName: string;
    sku: string;
    ownerId: string;
    ownerName: string;
    status: WorkspaceStatus;
    activatedDate: string;
    completionPercentage: number;
}

const today = new Date();

const addWeeks = (date: Date, weeks: number) => {
    const result = new Date(date);
    result.setDate(result.getDate() + weeks * 7);
    return result.toISOString().split('T')[0];
};

export const mockMasterPlanData: MasterPlanRecord[] = [
    // --- URGENT (< 4 WEEKS) ---
    {
        id: 1, projectName: 'Project Smaf', basic: 'SM-L345F_JPN_DCM', areaRegion: 'DCM', grade: 'D+', sku: 'SM-L345FZKJDCM', category: 'LPR', qtyLpr: 150, qtyLsr: 200,
        prePvrTargetDate: addWeeks(today, 1), praTargetDate: addWeeks(today, 5), sraTargetDate: addWeeks(today, 8), picIqc: 'SYN-0001', actionStatus: 'Urgent', isActivated: false
    },
    {
        id: 2, projectName: 'Project Q4 Lite', basic: 'SM-A156_MEA', areaRegion: 'MEA', grade: 'C', sku: 'SM-A156MZKIMEA', category: 'LPR', qtyLpr: 300, qtyLsr: 500,
        prePvrTargetDate: addWeeks(today, 2), praTargetDate: addWeeks(today, 6), sraTargetDate: addWeeks(today, 9), picIqc: 'SYN-0002', actionStatus: 'Urgent', isActivated: false
    },
    {
        id: 3, projectName: 'Galaxy A36 5G', basic: 'SM-A366B_EU', areaRegion: 'EU', grade: 'B', sku: 'SM-A366BZKAEUB', category: 'LPR', qtyLpr: 200, qtyLsr: 400,
        prePvrTargetDate: addWeeks(today, 3), praTargetDate: addWeeks(today, 7), sraTargetDate: addWeeks(today, 10), picIqc: '안병기', actionStatus: 'Urgent', isActivated: false
    },

    // --- READY (6 - 8 WEEKS) ---
    {
        id: 4, projectName: 'Fresh9', basic: 'SM-L335F_JPN_DCM', areaRegion: 'DCM', grade: 'D+', sku: 'SM-L335FZKIXOM', category: 'LPR', qtyLpr: 150, qtyLsr: 200,
        prePvrTargetDate: addWeeks(today, 6), praTargetDate: addWeeks(today, 10), sraTargetDate: addWeeks(today, 13), picIqc: 'SYN-0001', actionStatus: 'Ready', isActivated: false
    },
    {
        id: 5, projectName: 'Project V2', basic: 'SM-L715F_JPN_DCM', areaRegion: 'DCM', grade: 'D+', sku: 'SM-L715FZKJDCM', category: 'LPR', qtyLpr: 150, qtyLsr: 200,
        prePvrTargetDate: addWeeks(today, 7), praTargetDate: addWeeks(today, 11), sraTargetDate: addWeeks(today, 14), picIqc: '박군수', actionStatus: 'Ready', isActivated: false
    },
    {
        id: 6, projectName: 'Galaxy S25 FE', basic: 'SM-S721B_DS', areaRegion: 'GLB', grade: 'A', sku: 'SM-S721BZKADSM', category: 'LPR', qtyLpr: 500, qtyLsr: 1000,
        prePvrTargetDate: addWeeks(today, 7), praTargetDate: addWeeks(today, 12), sraTargetDate: addWeeks(today, 16), picIqc: 'SYN-0002', actionStatus: 'Ready', isActivated: false
    },
    {
        id: 7, projectName: 'Galaxy Z Flip 7', basic: 'SM-F751B', areaRegion: 'GLB', grade: 'Flagship', sku: 'SM-F751BZKAEUB', category: 'LPR', qtyLpr: 100, qtyLsr: 150,
        prePvrTargetDate: addWeeks(today, 8), praTargetDate: addWeeks(today, 14), sraTargetDate: addWeeks(today, 18), picIqc: 'SYN-0001', actionStatus: 'Ready', isActivated: false
    },

    // --- FUTURE (> 8 WEEKS) ---
    {
        id: 8, projectName: 'Tab S10 Ultra', basic: 'SM-X926B', areaRegion: 'GLB', grade: 'Flagship', sku: 'SM-X926BZKAEUB', category: 'LPR', qtyLpr: 50, qtyLsr: 100,
        prePvrTargetDate: addWeeks(today, 10), praTargetDate: addWeeks(today, 16), sraTargetDate: addWeeks(today, 20), picIqc: '안병기', actionStatus: 'Future', isActivated: false
    },
    {
        id: 9, projectName: 'Galaxy A16 5G', basic: 'SM-A166B', areaRegion: 'EU', grade: 'Entry', sku: 'SM-A166BZKAEUB', category: 'LPR', qtyLpr: 400, qtyLsr: 800,
        prePvrTargetDate: addWeeks(today, 12), praTargetDate: addWeeks(today, 16), sraTargetDate: addWeeks(today, 19), picIqc: '박군수', actionStatus: 'Future', isActivated: false
    },
    {
        id: 10, projectName: 'Galaxy Watch 8', basic: 'SM-R980', areaRegion: 'GLB', grade: 'Wearable', sku: 'SM-R980NZKAEUB', category: 'LPR', qtyLpr: 250, qtyLsr: 500,
        prePvrTargetDate: addWeeks(today, 15), praTargetDate: addWeeks(today, 20), sraTargetDate: addWeeks(today, 24), picIqc: 'SYN-0002', actionStatus: 'Future', isActivated: false
    },

    // --- ACTIVE (Đã khởi tạo) ---
    {
        id: 11, projectName: 'Galaxy Z Fold 7', basic: 'SM-F966B', areaRegion: 'GLB', grade: 'Flagship', sku: 'SM-F966BZKAEUB', category: 'LPR', qtyLpr: 150, qtyLsr: 200,
        prePvrTargetDate: '2026-08-15', praTargetDate: '2026-10-15', sraTargetDate: '2026-12-01', picIqc: 'SYN-0001', actionStatus: 'Active', isActivated: true
    },
    {
        id: 12, projectName: 'Galaxy S26 Ultra', basic: 'SM-S948B', areaRegion: 'GLB', grade: 'Flagship', sku: 'SM-S948BZKAEUB', category: 'LPR', qtyLpr: 300, qtyLsr: 500,
        prePvrTargetDate: '2026-09-01', praTargetDate: '2026-11-01', sraTargetDate: '2027-01-15', picIqc: 'SYN-0002', actionStatus: 'Active', isActivated: true
    }
];

export const mockActiveWorkspaces: ProjectWorkspace[] = [
    {
        id: 101,
        sourceRecordId: 11,
        projectName: 'Galaxy Z Fold 7',
        sku: 'SM-F966BZKAEUB',
        ownerId: 'SYN-0001',
        ownerName: 'Alex Tran',
        status: 'InProgress',
        activatedDate: '2026-06-15',
        completionPercentage: 45
    },
    {
        id: 102,
        sourceRecordId: 12,
        projectName: 'Galaxy S26 Ultra',
        sku: 'SM-S948BZKAEUB',
        ownerId: 'SYN-0002',
        ownerName: 'Jamie Nguyen',
        status: 'Preparation',
        activatedDate: '2026-07-01',
        completionPercentage: 10
    }
];
