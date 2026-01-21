import React from "react";
import { Container, Fade } from "@mui/material";
import { AdminUserRegistrationForm } from "./AdminUserRegistrationForm";

export interface AdminUserNewPageProps {
  onSuccess?: () => void;
}

export const AdminUserNewPage: React.FC<AdminUserNewPageProps> = ({ onSuccess }) => {
  return (
    <Fade in={true} timeout={400}>
      <Container maxWidth="sm" sx={{ mt: 4, mb: 6 }}>
        <AdminUserRegistrationForm
          onSuccess={() => {
            if (onSuccess) {
              onSuccess();
            }
          }}
        />
      </Container>
    </Fade>
  );
};
