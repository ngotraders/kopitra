import React from "react";
import { render, screen, waitFor, fireEvent } from "@testing-library/react";
import { vi, describe, it, afterEach, expect, skip } from "vitest";
import { ThemeProvider, createTheme } from "@mui/material/styles";
import { AdminUserManagementPage } from "../AdminUserManagementPage";

const mockListUsers = vi.fn();

vi.mock("../../../../api", () => ({
  userManagementApi: {
    listUsers: (...args: unknown[]) => mockListUsers(...args),
  },
}));

const renderWithTheme = (ui: React.ReactElement) => {
  const theme = createTheme();
  return render(<ThemeProvider theme={theme}>{ui}</ThemeProvider>);
};

const sampleUsers = [
  {
    id: "1",
    email: "alice@example.com",
    name: "Alice",
    canProvide: true,
    canSubscribe: true,
    isActive: true,
    registeredAt: "2025-01-01T00:00:00Z",
    lastLoginAt: "2025-01-02T00:00:00Z",
  },
  {
    id: "2",
    email: "bob@example.com",
    name: "Bob",
    canProvide: false,
    canSubscribe: true,
    isActive: false,
    registeredAt: "2025-01-05T00:00:00Z",
    lastLoginAt: null,
  },
];

describe("AdminUserManagementPage", () => {
  afterEach(() => {
    vi.clearAllMocks();
  });

  it.skip("renders users returned from API", async () => {
    mockListUsers.mockResolvedValueOnce({ users: sampleUsers, total: 2, page: 1, pageSize: 10 });

    renderWithTheme(<AdminUserManagementPage />);

    expect(mockListUsers).toHaveBeenCalled();

    await waitFor(() => {
      expect(screen.getByText("alice@example.com")).toBeInTheDocument();
      expect(screen.getByText("bob@example.com")).toBeInTheDocument();
    });
  });

  it.skip("calls API with search keyword", async () => {
    mockListUsers.mockResolvedValue({ users: sampleUsers, total: 2, page: 1, pageSize: 10 });

    renderWithTheme(<AdminUserManagementPage />);

    await waitFor(() => expect(mockListUsers).toHaveBeenCalled());
    mockListUsers.mockClear();

    fireEvent.change(screen.getByTestId("user-search"), { target: { value: "alice" } });

    await waitFor(() => {
      expect(mockListUsers).toHaveBeenCalledWith(1, 10, "alice");
    });
  });

  it.skip("shows error message when API fails", async () => {
    mockListUsers.mockRejectedValueOnce(new Error("network error"));

    renderWithTheme(<AdminUserManagementPage />);

    await waitFor(() => {
      expect(screen.getByTestId("user-error")).toHaveTextContent("network error");
    });
  });
});
