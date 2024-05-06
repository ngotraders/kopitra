import { Box, Card, CardContent, CardHeader } from "@mui/material";
import { List, TextField } from "react-admin";

export type DashboardProps = {

}

export const Dashboard = (props: DashboardProps) => {
  return (
    <Box>
      <Card>
        <CardHeader title="口座状態" />
        <CardContent>
          <List resource="accounts" filter={{ has_error: true }} disableSyncWithLocation>
            <TextField source="id" />
            <TextField source="name" />
          </List>
        </CardContent>
      </Card>
    </Box>
  );

}
