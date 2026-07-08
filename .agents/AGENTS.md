# PROJECT RULES - IQC Nexus

The following are project-scoped rules that must be followed for the IQC Nexus project.

<RULE[UI_UX_Interactivity]>
# UI/UX Interactivity: Personnel & PIC

Whenever designing or implementing a new module, table, card, or any UI element that assigns, displays, or involves a specific person (e.g., PIC, Owner, Approver, Executer):

- ALWAYS use the `UserBadge` component from `@/components/ui/user-badge`.
- Ensure the element is clickable and triggers a direct chat window (or an alert mocking the chat window for now) to facilitate immediate communication.
- Never use plain text or static avatar images for personnel without interaction capabilities.

</RULE[UI_UX_Interactivity]>
