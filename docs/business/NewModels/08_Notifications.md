# 08. Notification Layer

Every significant business event generates targeted notifications to ensure workflows do not stall.

## Business Events & Examples

- **DFx Overdue:** "DFx submission for Material X is 3 days past the deadline."
- **Vendor No Response:** "Vendor Y has not replied to the Capability Request."
- **Golden Sample Late:** "Golden Sample for Part Z has not been received."
- **Inspection Spec Completed:** "ISS for Part Z has been released."
- **BOM Updated:** "BOM for Model M has been updated by HQ. 5 new parts added."
- **Risk Added:** "A Critical Risk has been logged for Vendor Y."
- **MPPR Ready:** "Mass Production Pilot Run data is ready for Executive Review."

## Delivery Support Options

Notifications are delivered based on severity and user preference:

1. **Browser Notification:** Real-time push notifications while the app is open.
2. **Toast (In-App):** Brief, non-intrusive pop-ups in the bottom corner of the UI for success/informational events.
3. **Email:** Daily summaries or instant alerts for Critical events.
4. **Future Teams:** Integration with MS Teams / Slack for channel alerts.
5. **Future Mobile App:** Push notifications to iOS/Android devices for on-the-go approvals.
